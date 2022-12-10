namespace GameStore.Domain.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("DiscountCodes")]
    public partial class DiscountCode
    {
        [Key]
        public string Code { get; set; }

        [Required]
        public int Discount { get; set; }

    }
}
