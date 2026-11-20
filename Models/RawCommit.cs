using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitCommitScribe.Models
{
    /// <summary>
    /// git log çıktısından hiçbir işleme tabi tutulmadan ham olarak okunan tek bir commit kaydını (hash, başlık, gövde, yazar, tarih) temsil eder.
    /// Neden record : class yerine record kullanmamızın sebebi, commit nesnelerinin immutable (değiştirilemez) olmasını istememizdir. Commitler bir kez oluşturulduktan sonra değiştirilemezler; bu nedenle record kullanmak mantıklıdır.
    /// </summary>
    public sealed record RawCommit(
        string Hash,
        string ShortHash,
        string Subject,
        string Body,
        string Author,
        DateTimeOffset Date);
}
