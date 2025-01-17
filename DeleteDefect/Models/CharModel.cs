using System.ComponentModel.DataAnnotations;

namespace DeleteDefect.Models
{
    public class CharModel
    {
        [Key]
        public int id {  get; set; }
        public string Character { get; set; }
    }
}
