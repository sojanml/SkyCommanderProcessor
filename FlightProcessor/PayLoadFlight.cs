//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FlightProcessor
{
    using System;
    using System.Collections.Generic;
    
    public partial class PayLoadFlight
    {
        public int PayLoadFlightID { get; set; }
        public string FlightUniqueID { get; set; }
        public Nullable<System.DateTime> CreatedTime { get; set; }
        public Nullable<int> FlightID { get; set; }
        public Nullable<int> RFIDCount { get; set; }
        public string PayLoadImageFolder { get; set; }
        public Nullable<int> YardID { get; set; }
        public string ShelfID { get; set; }
        public Nullable<int> Processingmodel { get; set; }
        public Nullable<int> IsFlightSync { get; set; }
        public Nullable<int> PayLoadDroneID { get; set; }
    }
}