using System;
using System.Collections.Generic;
using UnityEngine;

// Unity-authored catalog container. Edit this asset in the Inspector; the runtime
// converts its serialized definitions into the same validated domain catalog used
// by JSON and future server adapters.
[CreateAssetMenu(menuName = "FunGuy/Content/Runtime Content Catalog", fileName = "UnityContentCatalog")]
public sealed class ContentCatalogAsset : ScriptableObject
{
    public int schemaVersion = 1;
    public string contentVersion = "unity-authored-v1";
    public CharactersFile characters = new CharactersFile();
    public SkillsFile skills = new SkillsFile();
    public StagesFile stages = new StagesFile();
    public BannersFile banners = new BannersFile();

    public void EnsureLists()
    {
        characters ??= new CharactersFile();
        characters.characters ??= new List<CharacterDef>();
        characters.enemies ??= new List<EnemyDef>();

        skills ??= new SkillsFile();
        skills.skills ??= new List<SkillDef>();

        stages ??= new StagesFile();
        stages.schemaVersion = stages.schemaVersion == 0 ? 1 : stages.schemaVersion;
        stages.stages ??= new List<StageDef>();

        foreach (var stage in stages.stages)
        {
            if (stage == null) continue;
            stage.waves ??= new List<WaveDef>();

            foreach (var wave in stage.waves)
            {
                if (wave == null) continue;
                wave.enemies ??= new List<WaveUnit>();
            }
        }

        banners ??= new BannersFile();
        banners.banners ??= new List<BannerDef>();
    }

    public void ValidateSchema()
    {
        EnsureLists();

        if (schemaVersion != 1)
            throw new InvalidOperationException($"Unsupported Unity catalog schema: {schemaVersion}.");

        if (stages.schemaVersion == 0)
            stages.schemaVersion = 1;
    }
}