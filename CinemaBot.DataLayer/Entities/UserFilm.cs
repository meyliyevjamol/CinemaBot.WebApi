using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CinemaBot.DataLayer.Entities;
[Table("sys_user_film")]
public class UserFilm
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }
    [Column("film_id")]
    public int FilmId { get; set; }

    [Column("chat_id")]
    public long ChatId { get; set; }
    [Column("added_key")]
    public bool AddedKey { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; }
    [ForeignKey("FilmId")]
    public virtual Film Film { get; set; }
}
