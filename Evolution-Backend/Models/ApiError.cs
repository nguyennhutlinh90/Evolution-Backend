using System.ComponentModel;

namespace Evolution_Backend.Models
{
    public enum ApiError
    {
        DbError = 1001,
        SystemError = 1002,
        LoginByOtherDevice = 1003,

        [Description("Missing {0}")]
        Missing = 2001,
        [Description("{0} is required")]
        Required = 2002,
        [Description("{0} are required")]
        Requireds = 2003,
        [Description("{0} is dupplicated in list")]
        Dupplicated = 2004,
        [Description("{0} is locked")]
        Locked = 2005,
        [Description("{0} is already in use on db context")]
        Existed = 2006,
        [Description("{0} was not found")]
        NotFound = 2007
    }
}
