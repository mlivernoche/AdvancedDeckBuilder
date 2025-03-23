using CardSourceGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedDeckBuilder.CardData;

public static class CardDataLoader
{
    public static IEnumerable<YGOCards.IYGOCard> LoadCardData()
    {
        var path = Path.Combine(Environment.CurrentDirectory, "CardData");
        var files = Directory.GetFiles(path, "*.json");
        return YGOCards
            .LoadAllCardDataFromYgoPro(files)
            .Values;
    }
}
