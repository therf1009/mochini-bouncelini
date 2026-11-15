using UnityEditor;
using UnityEngine;

/// <summary>
/// Setarile de import pentru sunete (se aplica singure cand pui un fisier in Resources/Audio):
/// muzica = streaming (nu sta toata in memorie), efectele si vocile = mono, decomprimate la incarcare (pornesc instant).
/// </summary>
public class AudioImportSettings : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.Contains("/Resources/Audio/")) return;
        var importer = (AudioImporter)assetImporter;
        var s = importer.defaultSampleSettings;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        if (assetPath.Contains("/Audio/Music/"))
        {
            s.loadType = AudioClipLoadType.Streaming;
            s.quality = 0.6f;
        }
        else
        {
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.quality = 0.7f;
            importer.forceToMono = true;
        }
        importer.defaultSampleSettings = s;
        importer.loadInBackground = assetPath.Contains("/Audio/Music/");
    }
}
