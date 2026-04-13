using System;

namespace CargoBot.Models
{
    public class UserParcel
    {
        public long ChatId { get; set; }
        public virtual User User { get; set; } = null!;

        public string TrackCode { get; set; } = null!;
        public virtual Parcel Parcel { get; set; } = null!;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}