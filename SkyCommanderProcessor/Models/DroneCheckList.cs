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
    
    public partial class DroneCheckList
    {
        public int ID { get; set; }
        public Nullable<int> DroneCheckListID { get; set; }
        public Nullable<int> DroneID { get; set; }
        public Nullable<int> FlightID { get; set; }
        public string Comments { get; set; }
        public Nullable<bool> IsOverride { get; set; }
        public string SignedBy { get; set; }
        public Nullable<int> CreatedBy { get; set; }
        public Nullable<System.DateTime> CreatedOn { get; set; }
    }
}
