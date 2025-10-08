using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVNetworkPacketAnalysisTool;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FFXIVNetworkPacketAnalysisTool.Utils;

public class OnlineOpcode
{
    private Configuration Configuration;
    private Plugin Plugin;
    public Dictionary<string, Dictionary<string, List<int>>> Opcodes { get; set; }
    public OnlineOpcode(Plugin plugin)
    {
        Plugin = plugin;
        Configuration = plugin.Configuration;
    }

    // 在 OnlineOpcode.cs 中

    public async void Run()
    {
        string gameVersion = null;
        unsafe { gameVersion = Framework.Framework.Instance()->GameVersionString; }
        Configuration.GameVersion = gameVersion;
        string jsonUrl = "https://raw.githubusercontent.com/extrant/FFXIV.EXE/main/Opcode/all_opcodes.json";

        // 从网络获取原始的 Opcode 数据
        var versionOpcodes = await GetOpcodesForVersion(jsonUrl, gameVersion, Plugin);

        if (versionOpcodes != null)
        {
            Plugin.ChatGui.Print($"{gameVersion} 联网Opcode获取到对应版本！");

            // 清空旧数据，准备填充新数据
            Configuration.UpOpcodes.Clear();
            Configuration.DownOpcodes.Clear();

            // 遍历获取到的数据，并根据前缀（"UP_" 或 "DOWN_"）进行分离
            foreach (var opcodeEntry in versionOpcodes)
            {
                var opcodeName = opcodeEntry.Key;
                var opcodeValues = opcodeEntry.Value;

                if (opcodeName.StartsWith("UP_"))
                {
                    foreach (var opcodeValue in opcodeValues)
                    {
                        Configuration.UpOpcodes[opcodeValue] = opcodeName;
                    }
                }
                else if (opcodeName.StartsWith("DOWN_"))
                {
                    foreach (var opcodeValue in opcodeValues)
                    {
                        Configuration.DownOpcodes[opcodeValue] = opcodeName;
                    }
                }
            }
            // 加载内置的(反正扫描不到不会加入)
            LocalOpcode.SetLocalUpOpcode(Configuration.UpOpcodes);

            Plugin.ChatGui.Print($"联网Opcode已加载！上行: {Configuration.UpOpcodes.Count} 个, 下行: {Configuration.DownOpcodes.Count} 个。");

        }
        else
        {
            Plugin.ChatGui.PrintError($"{gameVersion} 联网Opcode版本未找到！");

            // 清空数据以防使用旧的或错误的数据
            Configuration.UpOpcodes.Clear();
            Configuration.DownOpcodes.Clear();
        }
    }

    static async Task<Dictionary<string, List<int>>> GetOpcodesForVersion(string jsonUrl, string gameVersion, Plugin plugin)
    {
        try
        {
            string jsonContent = await GetJsonContent(jsonUrl);
            
            var opcodeData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<int>>>>(jsonContent);

            if (opcodeData != null && opcodeData.TryGetValue(gameVersion, out var versionOpcodes))
            {
                return versionOpcodes;
            }
        }
        catch (Exception ex)
        {
            Plugin.ChatGui.PrintError($"获取 Opcodes 时发生错误: {ex.Message}");
        }

        return null;
    }

    static async Task<string> GetJsonContent(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            return await client.GetStringAsync(url);
        }
    }
}
