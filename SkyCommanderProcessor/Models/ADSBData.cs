//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SkyCommanderProcessor.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class ADSBData
    {
        public int ID { get; set; }
        public Nullable<decimal> ADSBFlightDate { get; set; }
        public string ADSBFlightID { get; set; }
        public Nullable<decimal> ADSBLatitude { get; set; }
        public Nullable<decimal> ADSBLongitude { get; set; }
        public string ADSBIdent { get; set; }
        public Nullable<int> FlightID { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
    }
}
