using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBot.DataLayer.Entities;
[Table("sys_user_action_state")]
public class UserActionState
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }


    [Column("chat_id")]
    public long ChatId { get; set; }
    [Column("for_add_admin")]
    public bool ForAddAdmin { get; set; }
    [Column("for_add_channel")]
    public bool ForAddChannel { get; set; }
    [Column("for_add_film")]
    public bool ForAddFilm { get; set; }
    [Column("add_for_key")]
    public bool ForAddKey { get; set; }
    [Column("for_all_user_message")]
    public bool ForAllUserMessage { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; }
}
