#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Automatiza a geração da Build Windows Standalone (64-bit) e a criação do arquivo .zip
/// pronto para upload direto no itch.io (Requisito Mínimo nº 9 da Etapa 5).
/// Também salva automaticamente o prefab Resources/GameAudioManager.prefab com todos os
/// AudioClips da pasta Assets vinculados para que o som funcione perfeitamente no executável (.exe).
/// </summary>
public static class BuildAndItchGenerator
{
    private const string ResourcesDir = "Assets/Resources";
    private const string AudioPrefabPath = "Assets/Resources/GameAudioManager.prefab";

    [MenuItem("Chroma Defender/Gerar Build Windows (.zip para itch.io)", priority = 50)]
    public static void BuildWindowsAndZip()
    {
        try
        {
            // 1. Garante que o prefab do GameAudioManager exista em Assets/Resources com todos os .wav vinculados
            EnsureAudioPrefabForBuild();

            // 2. Garante a lista de cenas na ordem correta (MainMenu primeiro, depois as fases)
            string[] scenes = GetOrderedBuildScenes();
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Chroma Defender - Build",
                    "Nenhuma cena foi encontrada em Assets/Scenes!",
                    "OK");
                return;
            }

            // 3. Define as pastas de saída (fora da pasta Assets para não sujar o projeto Unity)
            string unityProjectDir = Directory.GetCurrentDirectory();
            string repoRootDir = Directory.GetParent(unityProjectDir)?.FullName ?? unityProjectDir;
            string buildRootDir = Path.Combine(repoRootDir, "Builds");
            string windowsBuildDir = Path.Combine(buildRootDir, "ChromaDefender_Windows");
            string exePath = Path.Combine(windowsBuildDir, "Chroma Defender.exe");
            string zipPath = Path.Combine(buildRootDir, "ChromaDefender_Windows_ItchIO.zip");

            if (Directory.Exists(windowsBuildDir))
                Directory.Delete(windowsBuildDir, true);
            Directory.CreateDirectory(windowsBuildDir);

            // 4. Executa o BuildPipeline para Windows 64-bit
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log($"[Chroma Defender] Iniciando Build Windows 64-bit com {scenes.Length} cena(s): {string.Join(", ", scenes)}");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Chroma Defender] Falha ao gerar a Build: {summary.result} ({summary.totalErrors} erros).");
                EditorUtility.DisplayDialog(
                    "Erro na Build",
                    $"A compilação não pôde ser concluída ({summary.result}). Verifique o Console da Unity.",
                    "OK");
                return;
            }

            // Remove a pasta BurstDebugInformation_DoNotShip (se a Unity criar), pois não deve ir para o itch.io
            foreach (string dir in Directory.GetDirectories(windowsBuildDir, "*DoNotShip*", SearchOption.TopDirectoryOnly))
            {
                try { Directory.Delete(dir, true); } catch { /* ignore */ }
            }

            // 5. Compacta automaticamente em .zip pronto para o itch.io
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(windowsBuildDir, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);

            long zipSizeMb = new FileInfo(zipPath).Length / (1024 * 1024);
            Debug.Log($"[Chroma Defender] Build e ZIP concluídos com sucesso! Arquivo: {zipPath} ({zipSizeMb} MB)");

            // Abre a pasta no Windows Explorer com o arquivo .zip selecionado
            EditorUtility.RevealInFinder(zipPath);

            EditorUtility.DisplayDialog(
                "Build Pronta para o itch.io!",
                $"Build Windows e arquivo .ZIP gerados com sucesso!\n\n" +
                $"Arquivo pronto para enviar no itch.io:\n{zipPath} ({zipSizeMb} MB)\n\n" +
                $"A pasta foi aberta no seu Explorador de Arquivos.",
                "Perfeito!");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Erro ao gerar Build/ZIP", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Cria/atualiza o prefab Assets/Resources/GameAudioManager.prefab com todos os AudioClips
    /// da pasta Gregor Quendel serializados, garantindo que o som funcione no executável (.exe).
    /// </summary>
    [MenuItem("Chroma Defender/Preparar Prefab de Áudio para Build", priority = 51)]
    public static void EnsureAudioPrefabForBuild()
    {
        if (!AssetDatabase.IsValidFolder(ResourcesDir))
            AssetDatabase.CreateFolder("Assets", "Resources");

        var tempGo = new GameObject("GameAudioManager");
        try
        {
            var mgr = tempGo.AddComponent<GameAudioManager>();
            mgr.PopulateEditorAudioAssets();
            PrefabUtility.SaveAsPrefabAsset(tempGo, AudioPrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Chroma Defender] Prefab de áudio preparado para Build em: {AudioPrefabPath}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(tempGo);
        }
    }

    private static string[] GetOrderedBuildScenes()
    {
        var list = new List<string>();

        const string mainMenu = "Assets/Scenes/MainMenu.unity";
        const string sampleScene = "Assets/Scenes/SampleScene.unity";

        if (File.Exists(mainMenu)) list.Add(mainMenu);
        if (File.Exists(sampleScene)) list.Add(sampleScene);

        // Inclui automaticamente novas cenas de fases que o Kauã adicionar (ex.: Fase2, Castelo, Poco...)
        foreach (var sceneSetting in EditorBuildSettings.scenes)
        {
            if (sceneSetting.enabled && File.Exists(sceneSetting.path) && !list.Contains(sceneSetting.path))
                list.Add(sceneSetting.path);
        }

        // Atualiza o EditorBuildSettings para refletir a lista final
        EditorBuildSettings.scenes = list.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        return list.ToArray();
    }
}
#endif
