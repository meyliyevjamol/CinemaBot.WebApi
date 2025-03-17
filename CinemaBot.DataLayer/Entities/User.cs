using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBot.DataLayer.Entities;

[Table("sys_user")]
public class User
{

    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("full_name")]
    [StringLength(100)]
    public string? FullName { get; set; }

    [Column("short_name")]
    [StringLength(100)]
    public string? ShortName { get; set; }

    [Column("chat_id")]
    public long ChatId { get; set; }
    [Column("is_admin")]
    public bool IsAdmin { get; set; }

    [Column("phone_number")]
    [StringLength(50)]
    public string? PhoneNumber { get; set; }
}

