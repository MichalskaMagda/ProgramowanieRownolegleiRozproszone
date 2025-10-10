using System;
using System.Threading;
using System.Threading.Tasks;
namespace SymulacjaGornikow 
{ 
    class Program 
    { 
        // --- Parametry symulacji ---
        static int rozmiarZloza = 2000; 
        static int pojemnoscPojazdu = 200; 
        static int czasWydobyciaJednostki = 10; 
        static int czasRozladowaniaJednostki = 10; 
        static int czasTransportu = 10000; // 10 s = 1000*10 = 10000 s 
        
        // --- Synchronizacja ---
        static SemaphoreSlim semaforZloze = new SemaphoreSlim(2, 2); // max 2 górników jednocześnie wydobywa
        static SemaphoreSlim semaforMagazyn = new SemaphoreSlim(1, 1); // max 1 górnik rozładowuje
        static object lockObject = new object(); // do synchronizacji wypisywania i aktualizacji zasobów
        
        static int stanMagazynu = 0; // ile węgla dostarczono
        static int numerGornika = 0; 
        static bool koniecSymulacji = false; 
        static string[] statusGornikow; 
        
        static void Main(string[] args) 
        { 
            int liczbaGornikow = 5; 
            statusGornikow = new string[liczbaGornikow]; 
            
            Console.CursorVisible = false; 
            Console.Clear(); 
            Console.WriteLine("Symulacja pracy górników rozpoczęta!\n"); 
            
            Task[] gornicy = new Task[liczbaGornikow]; 
            
            for (int i = 0; i < liczbaGornikow; i++) 
            { 
                int id = ++numerGornika; 
                gornicy[i] = Task.Run(() => PracaGornika(id -1, id)); } // Watek dla wyswietlacza
               
            Task wyswietlacz = Task.Run(() => UpdateDisplay(liczbaGornikow)); 
            Task.WaitAll(gornicy); koniecSymulacji = true; 

            wyswietlacz.Wait(); 

            Console.SetCursorPosition(0, liczbaGornikow + 5); 
            Console.CursorVisible = true; 
            Console.WriteLine("\nSymulacja zakończona!"); } 
        
        static void PracaGornika(int index, int id) 
        { 
            while (true) 
            { 
                int wydobyte = 0; // --- Próba wydobycia węgla ---
                
                semaforZloze.Wait(); 
                
                try 
                { 
                    lock (lockObject) 
                    { 
                        if (rozmiarZloza <= 0) 
                        { 
                            statusGornikow[index] = $"Górnik {id} kończy pracę."; 
                            return; // koniec – złoże puste
                        } 
                    } 
                    
                    statusGornikow[index] = $"Górnik {id} wydobywa węgiel..."; 
                    
                    while (wydobyte < pojemnoscPojazdu) 
                    { 
                        lock (lockObject) 
                        { 
                            if (rozmiarZloza <= 0) 
                                break; 
                            
                            rozmiarZloza--; wydobyte++; 
                        } 
                        
                        Thread.Sleep(czasWydobyciaJednostki); 
                    
                    } 
                }

                finally 
                { 
                    semaforZloze.Release(); 
                } 
                
                if (wydobyte == 0) break; // nic już nie zostało 
                
                // --- Transport do magazynu ---
                statusGornikow[index] = $"Górnik {id} transportuje węgiel do magazynu..."; 
                Thread.Sleep(czasTransportu); 
                
                // --- Rozładunek ---
                semaforMagazyn.Wait(); 

                try 
                { 
                    statusGornikow[index] = $"Górnik {id} rozładowuje węgiel..."; 
                    for (int i = 0; i < wydobyte; i++) 
                    { 
                        stanMagazynu++; Thread.Sleep(czasRozladowaniaJednostki); 
                    } 
                } 

                finally 
                { 
                    semaforMagazyn.Release(); 
                } 
            } statusGornikow[index] = $"Górnik {id} zakończył pracę."; 
        } 

        static void UpdateDisplay(int liczbaGornikow) 
        { 
            while (!koniecSymulacji) 
            { 
                lock (lockObject) 
                { 
                    Console.SetCursorPosition(0, 0); 
                    Console.WriteLine($"Stan złoża: {rozmiarZloza} jednostek węgla".PadRight(50)); 
                    Console.WriteLine($"Stan magazynu: {stanMagazynu} jednostek węgla".PadRight(50)); 
                    Console.WriteLine(new string('-', 50)); 
                    
                    for (int i = 0; i < liczbaGornikow; i++) 
                    { 
                        string status = statusGornikow[i] ?? $"Górnik {i + 1} czeka na zadanie."; 
                        
                        Console.WriteLine(status.PadRight(50)); 
                    } 
                } 
                Thread.Sleep(500); // odświeżanie co 0.5 sekundy
            } 
        } 
    } 
}