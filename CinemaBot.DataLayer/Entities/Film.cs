using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBot.DataLayer.Entities;

[Table("sys_film")]
public class Film
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("key")]
    [StringLength(100)]
    public string? Key { get; set; }

    [Column("telegram_channel_id")]
    public long TelegramChannelId { get; set; }
    [Column("message_id")]
    public int MessageId { get; set; }
    [Column("is_active")]
    public bool IsActive { get; set; }
}
