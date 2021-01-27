using System.Collections.Generic;

namespace Evolution_Backend.Models
{
    public class PL_Define_Request
    {
        public string ItemNumber { get; set; }

        public string ColorNumber { get; set; }

        public string Inseam { get; set; }

        public string Size { get; set; }

        public double ItemWeight { get; set; }
    }

    public class PL_Detail_Request
    {
        public string BoxNumber { get; set; }

        public string BoxDimension { get; set; }

        public double BoxWeight { get; set; }

        public string ItemNumber { get; set; }

        public string ColorNumber { get; set; }

        public string Inseam { get; set; }

        public string Size { get; set; }

        public double ItemWeight { get; set; }

        public double ExpectedQty { get; set; }
    }

    public class PL_Request
    {
        public string PLNumber { get; set; }

        public string PONumber { get; set; }

        public string CustomerCode { get; set; }

        public string StatusCode { get; set; }

        public bool ProcessManual { get; set; }

        public bool UseProduceQty { get; set; }

        public List<PL_Define_Request> Definies { get; set; }

        public List<PL_Detail_Request> Details { get; set; }
    }

    public class PL_Detail_Upload_Request
    {
        public string BoxNumber { get; set; }

        public string BoxStatusCode { get; set; }

        public string ItemNumber { get; set; }

        public string ColorNumber { get; set; }

        public string Inseam { get; set; }

        public string Size { get; set; }

        public double ExpectedQty { get; set; }

        public double PackedQty { get; set; }

        public string Note { get; set; }
    }

    public class PL_Upload_Request
    {
        public string PLNumber { get; set; }

        public string PONumber { get; set; }

        public List<PL_Detail_Upload_Request> Details { get; set; }
    }

    public class PL_ChangeStatus_Request
    {
        public string PLNumber { get; set; }

        public string PONumber { get; set; }

        public string StatusCode { get; set; }
    }

    public class PL_Lock_Request
    {
        public string PLNumber { get; set; }

        public string PONumber { get; set; }
    }

    public class PL_GetItem_Request
    {
        public string PLNumber { get; set; }

        public string PONumber { get; set; }
    }
}
