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
    
    public partial class AgriTraxImages
    {
        public int AgriTraxImageID { get; set; }
        public Nullable<int> AgriTraxID { get; set; }
        public Nullable<int> AgriTraxGroupID { get; set; }
        public string ImageFile { get; set; }
        public string Thumbnail { get; set; }
        public Nullable<decimal> Lat { get; set; }
        public Nullable<decimal> Lng { get; set; }
        public Nullable<System.DateTime> ImageDateTime { get; set; }
        public Nullable<System.DateTime> CreatedOn { get; set; }
        public Nullable<int> CreatedBy { get; set; }
    }
}
