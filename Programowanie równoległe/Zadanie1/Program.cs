using System;
using System.Threading;
using System.Threading.Tasks;

namespace SymulacjaGornikow
{
    class Program
    {
        // --- Parametry symulacji ---
        static int sharedResource = 2000;
        static int pojemnoscPojazdu = 200;
        static int czasWydobyciaJednostki = 10;
        static int czasRozladowaniaJednostki = 10;
        static int czasTransportu = 10000;       // 10 s = 1000*10 = 10000 s

        // --- Synchronizacja ---
        static SemaphoreSlim semaforZloze = new SemaphoreSlim(2, 2);  // max 2 górników jednocześnie wydobywa
        static SemaphoreSlim semaforMagazyn = new SemaphoreSlim(1, 1); // max 1 górnik rozładowuje
        static object lockObject = new object(); // do synchronizacji wypisywania i aktualizacji zasobów

        static int stanMagazynu = 0; // ile węgla dostarczono do magazynu
        static int numerGornika = 0; // do nadawania ID górnikom 

        static void Main(string[] args)
        {
            Console.WriteLine("Symulacja pracy rozpoczęta!\n");

            int liczbaGornikow = 5;

            Task[] gornicy = new Task[liczbaGornikow];
            for (int i = 0; i < liczbaGornikow; i++)
            {
                int id = ++numerGornika;
                gornicy[i] = Task.Run(() => PracaGornika(id));
            }

            Task.WaitAll(gornicy);

            Console.WriteLine("\nSymulacja zakończona!");
            Console.WriteLine($"Stan magazynu: {stanMagazynu}, pozostało w złożu: {sharedResource}");
        }

        static void PracaGornika(int id)
        {
            while (true)
            {
                int wydobyte = 0;

                // --- Próba wydobycia węgla ---
                semaforZloze.Wait();
                try
                {
                    lock (lockObject)
                    {
                        if (sharedResource <= 0)
                        {
                            return; // koniec – złoże puste
                        }
                    }

                    while (wydobyte < pojemnoscPojazdu)
                    {
                        lock (lockObject)
                        {
                            if (sharedResource <= 0)
                                break;
                            sharedResource--;
                            wydobyte++;
                        }
                        Thread.Sleep(czasWydobyciaJednostki);
                    }

                    lock (lockObject)
                    {
                        Console.WriteLine($"Górnik {id} wydobył {wydobyte} jednostek węgla. Pozostało : {sharedResource}");
                    }
                }
                finally
                {
                    semaforZloze.Release();
                }

                if (wydobyte == 0)
                    break; // nic już nie zostało do wydobycia

                // --- Transport do magazynu ---
                lock (lockObject)
                {
                    Console.WriteLine($"Górnik {id} transportuje złoże do magazynu...");
                }
                Thread.Sleep(czasTransportu);

                // --- Rozładunek ---
                semaforMagazyn.Wait();
                try
                {
                    lock (lockObject)
                    {
                        Console.WriteLine($"Górnik {id} rozładowuje złoże...");
                    }
                    for (int i = 0; i < wydobyte; i++)
                    {
                        stanMagazynu++;
                        Thread.Sleep(czasRozladowaniaJednostki);
                    }
                }
                finally
                {
                    semaforMagazyn.Release();
                }
            }
        }
    }
}
