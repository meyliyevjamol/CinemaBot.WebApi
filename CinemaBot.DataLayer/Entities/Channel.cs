using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBot.DataLayer.Entities;

[Table("sys_channel")]
public class Channel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("link")]
    [StringLength(100)]
    public string Link { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string? Name { get; set; }

    [Column("chat_id")]
    public long ChatId { get; set; }
    [Column("is_active")]
    public bool IsActive { get; set; }
}
