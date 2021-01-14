namespace Evolution_Backend.Models
{
    public class ItemBarcode_Request
    {
        public string Barcode { get; set; }

        public string ItemNumber { get; set; }

        public string ItemDescription { get; set; }

        public string ItemGroup { get; set; }

        public string Fit { get; set; }

        public string Style { get; set; }

        public string Season { get; set; }

        public string Quality { get; set; }

        public string Material { get; set; }

        public string ColorNumber { get; set; }

        public string ColorDescription { get; set; }

        public string Size { get; set; }

        public string Inseam { get; set; }

        public string TariffNumber { get; set; }
    }

    public class ItemBarcode_GetByBarcode_Request
    {
        public string Barcode { get; set; }
    }
}
