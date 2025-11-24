using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Snake
{
    pu class Program
    {
        public static List<ViewModelUserSettings> remoteIPAddress = new List<ViewModelUserSettings>();
        public static List<ViewModelGames> viewModelGames = new List<ViewModelGames>();
        public static List<Leaders> LeadersList = new List<Leaders>();
        public static int localPort = 5001;
        public static int MaxSpeed = 15;
        static void Main(string[] args)
        {
        }
    }
}
