using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Textures;
using Lumina.Excel;
using System;
namespace FFXIVNetworkPacketAnalysisTool.Utils
{
    public class TexturesHelper
    {

        public static IDalamudTextureWrap? GetTextureFromIconId(uint iconId, uint stackCount = 0, bool hdIcon = true)
        {
            GameIconLookup lookup = new GameIconLookup(iconId + stackCount, false, hdIcon);
            return Plugin.TextureProvider.GetFromGameIcon(lookup).GetWrapOrDefault();
        }

        public static IDalamudTextureWrap? GetTextureFromPath(string path)
        {
            return Plugin.TextureProvider.GetFromGame(path).GetWrapOrDefault();
        }
    }

}
