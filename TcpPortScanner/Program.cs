using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpPortScanner
{
    class Program
    {
        //Which portnumber we begin with (0 is actually reserved for other uses, but no harm in trying)
        private static int FirstPortToCheck = 0;
        //The last port we want to check, up too, but not including. (65536 is not a valid port)
        private static int LastPortToCheck = 65536;
        //How many ports each thread should check
        private static int PortsToCheckForEachThread = 1000;

        static void Main(string[] args)
        {
            Console.WriteLine("Port Scanner:");
            Console.WriteLine("write local for testing local ports or an IP address or a hostname for testing ports towards that");
            string address = Console.ReadLine();

            //The list to contain all the open ports
            List<int> openPorts = new List<int>();

            //Here we calculate how many threads we need using the class variables
            int numberOfThreads = (LastPortToCheck / PortsToCheckForEachThread);
            //If the number is divisible (modulus return more than 1), we add 1 to the number of thread, to include the last ports
            if (LastPortToCheck % PortsToCheckForEachThread > 0) { numberOfThreads++; }

            //Instead of a normal for loop, we can use the Parallel.For that creates threads for us
            //We can not be sure that the for loop runs sequentially, most likely it will be a random order
            Parallel.For(FirstPortToCheck, numberOfThreads, i =>
            {
                //calculate our beginning port
                int currentStartPort = i * PortsToCheckForEachThread;
                //calculate our last port to check
                int currentEndPort = Math.Min(LastPortToCheck, currentStartPort + PortsToCheckForEachThread);

                //If the user entered local (case-insensitive) we use the method TestLocalPorts
                if (address.Equals("local", StringComparison.CurrentCultureIgnoreCase))
                {
                    TestLocalPorts(currentStartPort, currentEndPort, openPorts);
                }
                //If the user entered anything else than local, we try to connect to that (not exception safe)
                else
                {
                    TestPorts(address, currentStartPort, currentEndPort, openPorts);
                }
            });

            //when all threads are done, we can ask the console to show us the number of open ports found, or show each open port found
            //This is also so the application doesn't stop, when there is no more in the main thread to do
            while (true)
            {
                string command = Console.ReadLine();

                switch (command.ToLower())
                {
                    case "count":
                        Console.WriteLine("Open ports found: " + openPorts.Count);
                        break;
                    case "show":
                        foreach (int openPort in openPorts)
                        {
                            Console.WriteLine("Following port is open: " + openPort);
                        }
                        break;
                }
            }
        }

        public static void TestPorts(string IpAddress, int startPort, int endPort, List<int> openPorts)
        {
            for (int i = startPort; i < endPort; i++)
            {
                try
                {
                    Console.WriteLine($"testing: {i} only {endPort - i} left!");
                    //when we initialize a TcpClient like this, it actually tries to establish a connection
                    //if no connection could be established, an exception is thrown, meaning the followeing 2 lines won't be executed in that instance
                    TcpClient tester = new TcpClient(IpAddress, i);
                    Console.WriteLine("Found open port: " + i);
                    openPorts.Add(i);
                }
                catch
                {
                    //Ignore the exception thrown, as that means the port wasn't open
                }
            }
        }

        public static void TestLocalPorts(int startPort, int endPort, List<int> openPorts)
        {
            for (int i = startPort; i < endPort; i++)
            {
                try
                {
                    Console.WriteLine($"testing: {i} only {endPort - i} left!");
                    //Initializes a listener on the port and on all network adapters
                    TcpListener listener = new TcpListener(System.Net.IPAddress.Any, i);
                    //if the port is already in use, this throws an exception
                    listener.Start();
                    //if no exception was thrown, we need to stop the listener again (to not use system resources etc.)
                    listener.Stop();
                }
                catch
                {
                    Console.WriteLine("Found open port: " + i);
                    //if the listerner.Start() line threw an exception, we assume it was because the port was already in use, meaning, something is listening on the port
                    openPorts.Add(i);
                }
            }
        }
    }
}
