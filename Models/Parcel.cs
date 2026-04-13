using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CargoBot.Models
{
    public class Parcel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string TrackCode { get; set; } = null!;

        public double Weight { get; set; }
        public DateTime ArrivedAtChina { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<UserParcel> SavedByUsers { get; set; } = new List<UserParcel>();
    }
}