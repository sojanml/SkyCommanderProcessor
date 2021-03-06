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
    
    public partial class MSTR_User
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public Nullable<bool> RememberMe { get; set; }
        public string PhotoUrl { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public Nullable<int> CreatedBy { get; set; }
        public Nullable<int> LastModifiedBy { get; set; }
        public Nullable<int> ApprovedBy { get; set; }
        public Nullable<int> UserProfileId { get; set; }
        public Nullable<int> UserAccountId { get; set; }
        public string Remarks { get; set; }
        public string MobileNo { get; set; }
        public string OfficeNo { get; set; }
        public string HomeNo { get; set; }
        public string EmailId { get; set; }
        public Nullable<int> CountryId { get; set; }
        public Nullable<int> RecordType { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<System.DateTime> LastModifiedOn { get; set; }
        public Nullable<System.DateTime> ApprovedOn { get; set; }
        public Nullable<System.DateTime> CreatedOn { get; set; }
        public string PasswordSalt { get; set; }
        public string Dashboard { get; set; }
        public Nullable<int> AccountId { get; set; }
        public Nullable<bool> IsPilot { get; set; }
        public string GeneratedPassword { get; set; }
        public string RPASPermitNo { get; set; }
        public string PermitCategory { get; set; }
        public string ContactAddress { get; set; }
        public string RegRPASSerialNo { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyTelephone { get; set; }
        public string CompanyEmail { get; set; }
        public string TradeLicenceCopyUrl { get; set; }
        public string EmiratesID { get; set; }
        public Nullable<System.DateTime> DOE_RPASPermit { get; set; }
        public Nullable<System.DateTime> DOI_RPASPermit { get; set; }
        public string UserDescription { get; set; }
        public string Nationality { get; set; }
        public string VideoKey { get; set; }
        public string ActivationKey { get; set; }
    }
}
