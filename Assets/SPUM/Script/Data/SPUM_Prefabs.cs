using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
public enum PlayerState
{
    IDLE,
    MOVE,
    ATTACK,
    DAMAGED,
    DEBUFF,
    DEATH,
    OTHER,
    DEFENSE,
}
public class SPUM_Prefabs : MonoBehaviour
{
    public float _version;
    public bool EditChk;
    public string _code;
    public Animator _anim;
    private AnimatorOverrideController OverrideController;

    public string UnitType;
    public List<SpumPackage> spumPackages = new List<SpumPackage>();
    public List<PreviewMatchingElement> ImageElement = new();
    public List<SPUM_AnimationData> SpumAnimationData = new();
    public Dictionary<string, List<AnimationClip>> StateAnimationPairs = new();
    public List<AnimationClip> IDLE_List = new();
    public List<AnimationClip> MOVE_List = new();
    public List<AnimationClip> ATTACK_List = new();
    public List<AnimationClip> DAMAGED_List = new();
    public List<AnimationClip> DEBUFF_List = new();
    public List<AnimationClip> DEATH_List = new();
    public List<AnimationClip> OTHER_List = new();
    public List<AnimationClip> DEFENSE_List = new();
    public void OverrideControllerInit()
    {
        Animator animator = _anim;
        OverrideController = new AnimatorOverrideController();
        OverrideController.runtimeAnimatorController= animator.runtimeAnimatorController;

        // 모든 애니메이션 클립을 가져옵니다
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

        foreach (AnimationClip clip in clips)
        {
            // 복제된 클립으로 오버라이드합니다
            OverrideController[clip.name] = clip;
        }

        animator.runtimeAnimatorController= OverrideController;
        foreach (PlayerState state in Enum.GetValues(typeof(PlayerState)))
        {
            var stateText = state.ToString();
            StateAnimationPairs[stateText] = new List<AnimationClip>();
            switch (stateText)
            {
                case "IDLE":
                    StateAnimationPairs[stateText] = IDLE_List;
                    break;
                case "MOVE":
                    StateAnimationPairs[stateText] = MOVE_List;
                    break;
                case "ATTACK":
                    StateAnimationPairs[stateText] = ATTACK_List;
                    break;
                case "DAMAGED":
                    StateAnimationPairs[stateText] = DAMAGED_List;
                    break;
                case "DEBUFF":
                    StateAnimationPairs[stateText] = DEBUFF_List;
                    break;
                case "DEATH":
                    StateAnimationPairs[stateText] = DEATH_List;
                    break;
                case "OTHER":
                    StateAnimationPairs[stateText] = OTHER_List;
                    break;
                case "DEFENSE":
                    StateAnimationPairs[stateText] = DEFENSE_List;
                    break;
            }
        }
    }
    public bool allListsHaveItemsExist(){
        List<List<AnimationClip>> allLists = new List<List<AnimationClip>>()
        {
            IDLE_List, MOVE_List, ATTACK_List, DAMAGED_List, DEBUFF_List, DEATH_List, OTHER_List, DEFENSE_List
        };

        return allLists.All(list => list.Count > 0);
    }
    [ContextMenu("PopulateAnimationLists")]
    public void PopulateAnimationLists()
    {
        IDLE_List = new();
        MOVE_List = new();
        ATTACK_List = new();
        DAMAGED_List = new();
        DEBUFF_List = new();
        DEATH_List = new();
        OTHER_List = new();
        DEFENSE_List = new();
        
        var groupedClips = spumPackages
        .SelectMany(package => package.SpumAnimationData)
        .Where(spumClip => spumClip.HasData && 
                        spumClip.UnitType.Equals(UnitType) && 
                        spumClip.index > -1 )
        .GroupBy(spumClip => spumClip.StateType)
        .ToDictionary(
            group => group.Key, 
            group => group.OrderBy(clip => clip.index).ToList()
        );
    // foreach (var item in groupedClips)
    // {
    //     foreach (var clip in item.Value)
    //     {
    //         Debug.Log(clip.ClipPath);
    //     }
    // }
        foreach (var kvp in groupedClips)
        {
            var stateType = kvp.Key;
            var orderedClips = kvp.Value;
            switch (stateType)
            {
                case "IDLE":
                    IDLE_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = IDLE_List;
                    break;
                case "MOVE":
                    MOVE_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = MOVE_List;
                    break;
                case "ATTACK":
                    ATTACK_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = ATTACK_List;
                    break;
                case "DAMAGED":
                    DAMAGED_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = DAMAGED_List;
                    break;
                case "DEBUFF":
                    DEBUFF_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = DEBUFF_List;
                    break;
                case "DEATH":
                    DEATH_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = DEATH_List;
                    break;
                case "OTHER":
                    OTHER_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = OTHER_List;
                    break;
                case "DEFENSE":
                    OTHER_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = OTHER_List;
                    break;
            }
        }
    
    }
    public void PlayAnimation(PlayerState playState, int index)
    {
        if (_anim == null || OverrideController == null)
        {
            Debug.LogWarning("Animator 또는 OverrideController가 설정되지 않았습니다.");
            return;
        }

        string stateName = playState.ToString();

        // 애니메이션 리스트 확인
        if (!StateAnimationPairs.ContainsKey(stateName))
        {
            Debug.LogWarning($"StateAnimationPairs에 '{stateName}' 키가 없습니다.");
            return;
        }

        var animations = StateAnimationPairs[stateName];
        if (animations == null || animations.Count == 0)
        {
            Debug.LogWarning($"'{stateName}'에 대한 애니메이션 클립이 비어 있습니다.");
            return;
        }

        if (index >= animations.Count)
        {
            Debug.LogWarning($"'{stateName}' index {index}는 범위를 초과합니다.");
            return;
        }

        AnimationClip clipToPlay = animations[index];
        if (clipToPlay == null)
        {
            Debug.LogWarning($"'{stateName}'의 index {index} 애니메이션이 null입니다.");
            return;
        }

        // 오버라이드 설정
        try
        {
            OverrideController[stateName] = clipToPlay;
        }
        catch (KeyNotFoundException)
        {
            Debug.LogWarning($"OverrideController에 '{stateName}' 키가 없습니다.");
            return;
        }

        // 상태별 bool 파라미터 설정
        bool isMove = stateName.Equals("MOVE", StringComparison.OrdinalIgnoreCase);
        bool isDebuff = stateName.Equals("DEBUFF", StringComparison.OrdinalIgnoreCase);
        bool isDeath = stateName.Equals("DEATH", StringComparison.OrdinalIgnoreCase);

        _anim.SetBool("1_Move", isMove);
        _anim.SetBool("5_Debuff", isDebuff);
        _anim.SetBool("isDeath", isDeath);

        // Trigger 파라미터 처리 (MOVE/DEBUFF 제외 시 발동)
        if (!isMove && !isDebuff)
        {
            foreach (var parameter in _anim.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    if (parameter.name.Equals(stateName, StringComparison.OrdinalIgnoreCase))
                    {
                        _anim.SetTrigger(parameter.name);
                        break;
                    }
                }
            }
        }
    }
    AnimationClip LoadAnimationClip(string clipPath)
    {
        // "Animations" 폴더에서 애니메이션 클립 로드
        AnimationClip clip = Resources.Load<AnimationClip>(clipPath.Replace(".anim", ""));
        
        if (clip == null)
        {
            Debug.LogWarning($"Failed to load animation clip '{clipPath}'.");
        }
        
        return clip;
    }
}
