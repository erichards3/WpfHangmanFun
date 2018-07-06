using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HangmanFunWPF.Classes
{
  [Serializable]
  public class RandomWord
  {
    /* This is a Json Serializable object for use with the Newtonsoft.Json library.
     * By specifying the JsonProperty, Newtonsoft can implicitly deserialize a Json
     * object to this C#.Net object with the same properties. */
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("word")]
    public string Word { get; set; }
  }
}
