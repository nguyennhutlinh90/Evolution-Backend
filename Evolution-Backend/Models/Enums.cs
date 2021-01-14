namespace Evolution_Backend.Models
{
    public class StatusEnums
    {
        public enum General
        {
            Active = 1,
            Deactive = 2
        }

        public enum PO
        {
            Open = 1, // Draft
            Ready = 2, // Create packing list
            Active = 3, // PDA downloaded packing list
            PartialPacked = 4,
            Packed = 5, // Upload packing list
            Shipped = 6
        }

        public enum PL
        {
            Draft = 1,
            Ready = 2,
            Active = 3,
            InProgress = 4,
            Done = 5
        }

        public enum PL_PO
        {
            Open = 1, // Draft
            Ready = 2, // Create packing list
            Active = 3, // PDA downloaded packing list
            PartialPacked = 4,
            Packed = 5, // Upload packing list
            Shipped = 6
        }

        public enum PL_Box
        {
            Open = 1,
            Done = 2
        }

        public enum PL_Qty
        {
            Diff = 1,
            Match = 2
        }
    }

    public class TypeEnums
    {
        public enum Action
        {
            POCreate = 1001,
            PODelete = 1002,
            POUpdate = 1003,
            POUpload = 1004,

            PLCreate = 2001,
            PLDelete = 2002,
            PLUpdate = 2003,
            PLUpload = 2004
        }
    }
}
