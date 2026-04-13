using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CargoBot.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long ChatId { get; set; }

        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<UserParcel> SavedParcels { get; set; } = new List<UserParcel>();
    }
}