using System.Collections.Generic;

namespace Evolution_Backend.Models
{
    public class PO_Detail_Upload_Request
    {
        public string ItemNumber { get; set; }

        public string ItemDescription { get; set; }

        public string ColorNumber { get; set; }

        public string ColorDescription { get; set; }

        public string Inseam { get; set; }

        public string Size { get; set; }

        public double OriginalQty { get; set; }

        public double AdditionalQty { get; set; }

        public double Price { get; set; }

        public string TariffNumber { get; set; }

        public string Quality { get; set; }

        public string Material { get; set; }
    }

    public class PO_Upload_Request
    {
        public string PONumber { get; set; }

        public string PODate { get; set; }

        public string ETA { get; set; }

        public string ETD { get; set; }

        public string PaymentTerms { get; set; }

        public string Packing { get; set; }

        public string Ship { get; set; }

        public List<PO_Detail_Upload_Request> Details { get; set; }
    }

    public class PO_Update_Request
    {
        public string PONumber { get; set; }

        public string ETA { get; set; }

        public string ETD { get; set; }

        public string Packing { get; set; }

        public string Ship { get; set; }
    }

    public class PO_Update_ItemQuantity_Request
    {
        public string PONumber { get; set; }

        public string Barcode { get; set; }

        public double OriginalQty { get; set; }

        public double AdditionalQty { get; set; }
    }
}
