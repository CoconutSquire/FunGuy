using System;
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
        characters.characters ??= new System.Collections.Generic.List<CharacterDef>();
        characters.enemies ??= new System.Collections.Generic.List<EnemyDef>();
        skills ??= new SkillsFile();
        skills.skills ??= new System.Collections.Generic.List<SkillDef>();
        stages ??= new StagesFile();
        stages.stages ??= new System.Collections.Generic.List<StageDef>();
        banners ??= new BannersFile();
        banners.banners ??= new System.Collections.Generic.List<BannerDef>();
    }

    public void ValidateSchema()
    {
        EnsureLists();
        if (schemaVersion != 1) throw new InvalidOperationException($"Unsupported Unity catalog schema: {schemaVersion}.");
        if (stages.schemaVersion == 0) stages.schemaVersion = 1;
    }
}
