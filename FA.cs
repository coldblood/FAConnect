using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBot
{
    public class FA
    {
        public Summary summary { get; set; }
        public Client[] clients { get; set; }
        public Alert[] alerts { get; set; }
    }

    public class Summary
    {
        public int clients { get; set; }
        public string avgPortfolio { get; set; }
        public int planOnTrack { get; set; }
        public int planOffTrack { get; set; }
        public int criticalAlerts { get; set; }
    }

    public class Client
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public int age { get; set; }
        public int retAge { get; set; }
        public int id { get; set; }
        public int portfolioValue { get; set; }
        public Accounts accounts { get; set; }
        public Plan[] plans { get; set; }
    }

    public class Accounts
    {
        public int _internal { get; set; }
        public int external { get; set; }
    }

    public class Plan
    {
        public string name { get; set; }
        public int essentialGoalAmount { get; set; }
        public int discretionaryGoalAmount { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public float TargetPOS { get; set; }
        public int totalGoalAmount { get; set; }
        public object alerts { get; set; }
    }

    public class Alert
    {
        public int id { get; set; }
        public int clientId { get; set; }
        public string type { get; set; }
        public string message { get; set; }
    }
}
