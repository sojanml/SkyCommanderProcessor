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
    
    public partial class DroneDefinition
    {
        public int Id { get; set; }
        public string FieldName { get; set; }
        public string BitLength { get; set; }
        public Nullable<int> CreatedBy { get; set; }
        public Nullable<int> LastModifiedBy { get; set; }
        public Nullable<System.DateTime> CreatedOn { get; set; }
        public Nullable<int> RecordType { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<System.DateTime> LastModifiedOn { get; set; }
        public Nullable<int> MaxValue { get; set; }
        public string DefaultValue { get; set; }
        public Nullable<int> ParentID { get; set; }
    }
}
