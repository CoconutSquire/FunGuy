using System;
using System.Collections.Generic;
using UnityEngine;

// Unity-authored catalog container. Edit this asset in the Inspector; runtime code
// always consumes a deep-cloned snapshot, never these serialized objects directly.
[CreateAssetMenu(menuName = "FunGuy/Content/Runtime Content Catalog", fileName = "UnityContentCatalog")]
public sealed class ContentCatalogAsset : ScriptableObject
{
    public int schemaVersion = 1;
    public string contentVersion = "unity-authored-v1";
    public CharactersFile characters = new CharactersFile();
    public SkillsFile skills = new SkillsFile();
    public StagesFile stages = new StagesFile();
    public BannersFile banners = new BannersFile();

    // Editor-authoring convenience only. Runtime loading intentionally does not call
    // this method because silently creating missing lists would hide a damaged asset.
    public void EnsureListsForEditing()
    {
        characters ??= new CharactersFile();
        characters.characters ??= new List<CharacterDef>();
        characters.enemies ??= new List<EnemyDef>();

        skills ??= new SkillsFile();
        skills.skills ??= new List<SkillDef>();

        stages ??= new StagesFile();
        if (stages.schemaVersion == 0) stages.schemaVersion = 1;
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
        if (schemaVersion != 1)
            throw new InvalidOperationException($"Unsupported Unity catalog schema: {schemaVersion}.");
        if (characters == null || skills == null || stages == null || banners == null)
            throw new InvalidOperationException("Unity content catalog is missing a top-level content section.");
        if (stages.schemaVersion != 1)
            throw new InvalidOperationException($"Unsupported stage schema in Unity catalog: {stages.schemaVersion}.");
    }
}
