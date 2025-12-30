using UnityEngine;
using System.Collections.Generic;

public class OutfitManager : MonoBehaviour
{
    public GameObject head_wrapper;
    GameObject[] head_outfits;

    public GameObject body_wrapper;
    GameObject[] body_outfits;

    public GameObject waist_wrapper;
    GameObject[] waist_outfits;

    public GameObject left_arm_wrapper;
    public GameObject right_arm_wrapper;
    GameObject[] left_arm_outfits;
    GameObject[] right_arm_outfits;

    public GameObject left_thigh_wrapper;
    public GameObject right_thigh_wrapper;
    GameObject[] left_thigh_outfits;
    GameObject[] right_thigh_outfits;

    public GameObject left_shin_wrapper;
    public GameObject right_shin_wrapper;
    GameObject[] left_shin_outfits;
    GameObject[] right_shin_outfits;

    public GameObject left_foot_wrapper;
    public GameObject right_foot_wrapper;
    GameObject[] left_foot_outfits;
    GameObject[] right_foot_outfits;

    public GameObject board_wrapper;
    GameObject[] board_outfits;

    int head_index;
    int body_index;
    int waist_index;
    int arm_index;
    int leg_index;
    int foot_index;
    int board_index;

    void Start()
    {
        head_outfits = CollectChildOutfits(head_wrapper);
        body_outfits = CollectChildOutfits(body_wrapper);
        waist_outfits = CollectChildOutfits(waist_wrapper);

        left_arm_outfits = CollectChildOutfits(left_arm_wrapper);
        right_arm_outfits = CollectChildOutfits(right_arm_wrapper);

        left_thigh_outfits = CollectChildOutfits(left_thigh_wrapper);
        right_thigh_outfits = CollectChildOutfits(right_thigh_wrapper);

        left_shin_outfits = CollectChildOutfits(left_shin_wrapper);
        right_shin_outfits = CollectChildOutfits(right_shin_wrapper);

        left_foot_outfits = CollectChildOutfits(left_foot_wrapper);
        right_foot_outfits = CollectChildOutfits(right_foot_wrapper);

        board_outfits = CollectChildOutfits(board_wrapper);

        LogCollectedOutfits();

        head_index = InitializeOutfits(head_outfits, null);
        body_index = InitializeOutfits(body_outfits, null);
        waist_index = InitializeOutfits(waist_outfits, null);
        arm_index = InitializeOutfits(left_arm_outfits, right_arm_outfits);
        AlignArmsToBody();
        leg_index = InitializeOutfits(left_thigh_outfits, right_thigh_outfits);
        AlignShinsToLegs();
        AlignWaistToLegs();
        foot_index = InitializeOutfits(left_foot_outfits, right_foot_outfits);
        board_index = InitializeOutfits(board_outfits, null);
    }

    void Update()
    {
    }

    public void SelectNextHead()
    {
        EnsureOutfits(ref head_outfits, head_wrapper, "head");
        SwitchOutfit(head_outfits, null, ref head_index, 1);
    }

    public void SelectPrevHead()
    {
        EnsureOutfits(ref head_outfits, head_wrapper, "head");
        SwitchOutfit(head_outfits, null, ref head_index, -1);
    }

    public void SelectNextBody()
    {
        EnsureOutfits(ref body_outfits, body_wrapper, "body");
        EnsurePairedOutfits(ref left_arm_outfits, left_arm_wrapper, ref right_arm_outfits, right_arm_wrapper, "arms");
        SwitchBodyWithArms(ref body_index, 1);
    }

    public void SelectPrevBody()
    {
        EnsureOutfits(ref body_outfits, body_wrapper, "body");
        EnsurePairedOutfits(ref left_arm_outfits, left_arm_wrapper, ref right_arm_outfits, right_arm_wrapper, "arms");
        SwitchBodyWithArms(ref body_index, -1);
    }

    public void SelectNextWaist()
    {
        EnsureOutfits(ref waist_outfits, waist_wrapper, "waist");
        SwitchOutfit(waist_outfits, null, ref waist_index, 1);
    }

    public void SelectPrevWaist()
    {
        EnsureOutfits(ref waist_outfits, waist_wrapper, "waist");
        SwitchOutfit(waist_outfits, null, ref waist_index, -1);
    }

    public void SelectNextArms()
    {
        EnsurePairedOutfits(ref left_arm_outfits, left_arm_wrapper, ref right_arm_outfits, right_arm_wrapper, "arms");
        SwitchOutfit(left_arm_outfits, right_arm_outfits, ref arm_index, 1);
    }

    public void SelectPrevArms()
    {
        EnsurePairedOutfits(ref left_arm_outfits, left_arm_wrapper, ref right_arm_outfits, right_arm_wrapper, "arms");
        SwitchOutfit(left_arm_outfits, right_arm_outfits, ref arm_index, -1);
    }

    public void SelectNextLegs()
    {
        EnsurePairedOutfits(ref left_thigh_outfits, left_thigh_wrapper, ref right_thigh_outfits, right_thigh_wrapper, "thighs");
        EnsurePairedOutfits(ref left_shin_outfits, left_shin_wrapper, ref right_shin_outfits, right_shin_wrapper, "shins");
        EnsureOutfits(ref waist_outfits, waist_wrapper, "waist");
        SwitchLegs(ref leg_index, 1);
    }

    public void SelectPrevLegs()
    {
        EnsurePairedOutfits(ref left_thigh_outfits, left_thigh_wrapper, ref right_thigh_outfits, right_thigh_wrapper, "thighs");
        EnsurePairedOutfits(ref left_shin_outfits, left_shin_wrapper, ref right_shin_outfits, right_shin_wrapper, "shins");
        EnsureOutfits(ref waist_outfits, waist_wrapper, "waist");
        SwitchLegs(ref leg_index, -1);
    }

    // Backwards compatibility in case buttons are still wired to old methods
    public void SelectNextThighs() => SelectNextLegs();
    public void SelectPrevThighs() => SelectPrevLegs();
    public void SelectNextShins() => SelectNextLegs();
    public void SelectPrevShins() => SelectPrevLegs();

    public void SelectNextFeet()
    {
        EnsurePairedOutfits(ref left_foot_outfits, left_foot_wrapper, ref right_foot_outfits, right_foot_wrapper, "feet");
        SwitchOutfit(left_foot_outfits, right_foot_outfits, ref foot_index, 1);
    }

    public void SelectPrevFeet()
    {
        EnsurePairedOutfits(ref left_foot_outfits, left_foot_wrapper, ref right_foot_outfits, right_foot_wrapper, "feet");
        SwitchOutfit(left_foot_outfits, right_foot_outfits, ref foot_index, -1);
    }

    public void SelectNextBoard()
    {
        EnsureOutfits(ref board_outfits, board_wrapper, "board");
        SwitchOutfit(board_outfits, null, ref board_index, 1);
    }

    public void SelectPrevBoard()
    {
        EnsureOutfits(ref board_outfits, board_wrapper, "board");
        SwitchOutfit(board_outfits, null, ref board_index, -1);
    }

    void LogCollectedOutfits()
    {
        Debug.Log($"[OutfitManager] Head: {BuildNameList(head_outfits)}");
        Debug.Log($"[OutfitManager] Body: {BuildNameList(body_outfits)}");
        Debug.Log($"[OutfitManager] Waist: {BuildNameList(waist_outfits)}");
        Debug.Log($"[OutfitManager] Arms: L[{BuildNameList(left_arm_outfits)}] R[{BuildNameList(right_arm_outfits)}]");
        Debug.Log($"[OutfitManager] Thighs: L[{BuildNameList(left_thigh_outfits)}] R[{BuildNameList(right_thigh_outfits)}]");
        Debug.Log($"[OutfitManager] Shins: L[{BuildNameList(left_shin_outfits)}] R[{BuildNameList(right_shin_outfits)}]");
        Debug.Log($"[OutfitManager] Feet: L[{BuildNameList(left_foot_outfits)}] R[{BuildNameList(right_foot_outfits)}]");
        Debug.Log($"[OutfitManager] Board: {BuildNameList(board_outfits)}");
    }

    string BuildNameList(GameObject[] outfits)
    {
        if (outfits == null || outfits.Length == 0)
        {
            return "none";
        }

        List<string> names = new List<string>();
        for (int i = 0; i < outfits.Length; i++)
        {
            names.Add(outfits[i] == null ? "null" : outfits[i].name);
        }

        return string.Join(", ", names);
    }

    GameObject[] CollectChildOutfits(GameObject wrapper)
    {
        if (wrapper == null)
        {
            return new GameObject[0];
        }

        List<GameObject> outfits = new List<GameObject>();
        for (int i = 0; i < wrapper.transform.childCount; i++)
        {
            GameObject child = wrapper.transform.GetChild(i).gameObject;
            if (HasLetterPrefix(child.name))
            {
                outfits.Add(child);
            }
        }

        return outfits.ToArray();
    }

    bool HasLetterPrefix(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length < 2)
        {
            return false;
        }

        return char.IsLetter(name[0]) && name[1] == '_';
    }

    int InitializeOutfits(GameObject[] primary, GameObject[] secondary)
    {
        int maxLength = MaxUsableLength(primary, secondary);
        if (maxLength == 0)
        {
            return 0;
        }

        int activeIndex = FindFirstActive(primary, maxLength);
        if (activeIndex == -1)
        {
            activeIndex = FindFirstActive(secondary, maxLength);
        }

        if (activeIndex == -1)
        {
            activeIndex = 0;
        }

        SetActiveOnlyAll(primary, activeIndex);
        SetActiveOnlyAll(secondary, activeIndex);

        Debug.Log($"[OutfitManager] InitializeOutfits -> activeIndex {activeIndex} | primary {BuildIndexName(primary, activeIndex)} | secondary {BuildIndexName(secondary, activeIndex)}");

        return activeIndex;
    }

    int MaxUsableLength(GameObject[] primary, GameObject[] secondary)
    {
        int primaryLength = primary?.Length ?? 0;
        int secondaryLength = secondary?.Length ?? 0;

        int maxLength = Mathf.Max(primaryLength, secondaryLength);

        Debug.Log($"[OutfitManager] MaxUsableLength -> primaryLen {primaryLength} secondaryLen {secondaryLength} maxLen {maxLength}");

        return maxLength;
    }

    int FindFirstActive(GameObject[] outfits, int maxLength)
    {
        if (outfits == null)
        {
            return -1;
        }

        int limit = Mathf.Min(outfits.Length, maxLength);
        for (int i = 0; i < limit; i++)
        {
            if (outfits[i] != null && outfits[i].activeSelf)
            {
                return i;
            }
        }

        return -1;
    }

    void SetActiveOnlyAll(GameObject[] outfits, int indexToActivate)
    {
        if (outfits == null)
        {
            return;
        }

        for (int i = 0; i < outfits.Length; i++)
        {
            if (outfits[i] != null)
            {
                bool shouldActivate = (i == indexToActivate);
                Debug.Log($"[OutfitManager] SetActiveOnlyAll -> idx {i} name {outfits[i].name} setActive {shouldActivate}");
                outfits[i].SetActive(i == indexToActivate);
            }
        }

        LogActiveStates("after SetActiveOnlyAll", outfits);
    }


    void SwitchOutfit(GameObject[] primary, GameObject[] secondary, ref int currentIndex, int direction)
    {
        int maxLength = MaxUsableLength(primary, secondary);
        Debug.Log($"[OutfitManager] SwitchOutfit -> maxLength {maxLength} | primary {BuildNameList(primary)} | secondary {BuildNameList(secondary)} | currentIndex {currentIndex} | direction {direction}");
        if (maxLength == 0)
        {
            Debug.LogWarning("[OutfitManager] SwitchOutfit aborted because maxLength is 0.");
            return;
        }

        currentIndex = NormalizeIndex(currentIndex, maxLength);

        int detectedActivePrimary = FindFirstActive(primary, maxLength);
        int detectedActiveSecondary = FindFirstActive(secondary, maxLength);

        if (detectedActivePrimary != -1 || detectedActiveSecondary != -1)
        {
            int resolvedActive = detectedActivePrimary != -1 ? detectedActivePrimary : detectedActiveSecondary;
            if (detectedActivePrimary != -1 && detectedActiveSecondary != -1)
            {
                resolvedActive = Mathf.Min(detectedActivePrimary, detectedActiveSecondary);
            }

            if (resolvedActive != currentIndex)
            {
                Debug.Log($"[OutfitManager] SwitchOutfit -> syncing currentIndex from {currentIndex} to detected active {resolvedActive}");
                currentIndex = resolvedActive;
            }
        }

        LogActiveStates("before switch primary", primary);
        LogActiveStates("before switch secondary", secondary);

        currentIndex = NormalizeIndex(currentIndex + direction, maxLength);

        Debug.Log($"[OutfitManager] SwitchOutfit -> dir {direction} newIndex {currentIndex} | primary {BuildIndexName(primary, currentIndex)} | secondary {BuildIndexName(secondary, currentIndex)} | maxLen {maxLength}");

        SetActiveOnlyAll(primary, currentIndex);
        SetActiveOnlyAll(secondary, currentIndex);

        LogActiveStates("after switch primary", primary);
        LogActiveStates("after switch secondary", secondary);
    }

    void SwitchBodyWithArms(ref int currentIndex, int direction)
    {
        int bodyLength = MaxUsableLength(body_outfits, null);
        int armLength = MaxUsableLength(left_arm_outfits, right_arm_outfits);
        int maxLength = ResolvePairLength(bodyLength, armLength);

        Debug.Log($"[OutfitManager] SwitchBodyWithArms -> maxLength {maxLength} | body {BuildNameList(body_outfits)} | arms {BuildNameList(left_arm_outfits)} | currentIndex {currentIndex} | direction {direction}");

        if (maxLength == 0)
        {
            Debug.LogWarning("[OutfitManager] SwitchBodyWithArms aborted because maxLength is 0.");
            return;
        }

        currentIndex = NormalizeIndex(currentIndex, maxLength);

        int detectedBody = FindFirstActive(body_outfits, bodyLength);
        int detectedArms = FindFirstActive(left_arm_outfits, armLength);
        int resolvedActive = detectedBody != -1 ? detectedBody : detectedArms;
        if (detectedBody != -1 && detectedArms != -1)
        {
            resolvedActive = Mathf.Min(detectedBody, detectedArms);
        }

        if (resolvedActive != -1 && resolvedActive != currentIndex)
        {
            Debug.Log($"[OutfitManager] SwitchBodyWithArms -> syncing currentIndex from {currentIndex} to detected active {resolvedActive}");
            currentIndex = resolvedActive;
        }

        LogActiveStates("before switch body", body_outfits);
        LogActiveStates("before switch arms", left_arm_outfits);

        currentIndex = NormalizeIndex(currentIndex + direction, maxLength);

        int armIndex = NormalizeIndex(currentIndex, armLength == 0 ? 1 : armLength);
        arm_index = armIndex;

        Debug.Log($"[OutfitManager] SwitchBodyWithArms -> dir {direction} newIndex {currentIndex} | armIndex {armIndex} | maxLen {maxLength}");

        SetActiveOnlyAll(body_outfits, currentIndex);

        SetActiveOnlyAll(left_arm_outfits, armIndex);
        SetActiveOnlyAll(right_arm_outfits, armIndex);

        LogActiveStates("after switch body", body_outfits);
        LogActiveStates("after switch arms", left_arm_outfits);
    }

    void SwitchLegs(ref int currentIndex, int direction)
    {
        int thighLength = MaxUsableLength(left_thigh_outfits, right_thigh_outfits);
        int shinLength = MaxUsableLength(left_shin_outfits, right_shin_outfits);
        int waistLength = MaxUsableLength(waist_outfits, null);

        int legLength = ResolvePairLength(thighLength, shinLength);
        int maxLength = ResolvePairLength(legLength, waistLength);

        Debug.Log($"[OutfitManager] SwitchLegs -> maxLength {maxLength} | thighs {BuildNameList(left_thigh_outfits)} | shins {BuildNameList(left_shin_outfits)} | waist {BuildNameList(waist_outfits)} | currentIndex {currentIndex} | direction {direction}");

        if (maxLength == 0)
        {
            Debug.LogWarning("[OutfitManager] SwitchLegs aborted because maxLength is 0.");
            return;
        }

        currentIndex = NormalizeIndex(currentIndex, maxLength);

        int detectedActiveThigh = FindFirstActive(left_thigh_outfits, thighLength);
        int detectedActiveShin = FindFirstActive(left_shin_outfits, shinLength);
        int detectedActiveWaist = FindFirstActive(waist_outfits, waistLength);

        int resolvedActive = detectedActiveThigh != -1 ? detectedActiveThigh : detectedActiveShin;
        if (detectedActiveThigh != -1 && detectedActiveShin != -1)
        {
            resolvedActive = Mathf.Min(detectedActiveThigh, detectedActiveShin);
        }

        if (resolvedActive == -1 && detectedActiveWaist != -1)
        {
            resolvedActive = detectedActiveWaist;
        }
        else if (resolvedActive != -1 && detectedActiveWaist != -1)
        {
            resolvedActive = Mathf.Min(resolvedActive, detectedActiveWaist);
        }

        if (resolvedActive != -1 && resolvedActive != currentIndex)
        {
            Debug.Log($"[OutfitManager] SwitchLegs -> syncing currentIndex from {currentIndex} to detected active {resolvedActive}");
            currentIndex = resolvedActive;
        }

        LogActiveStates("before switch thighs", left_thigh_outfits);
        LogActiveStates("before switch shins", left_shin_outfits);
        LogActiveStates("before switch waist", waist_outfits);

        currentIndex = NormalizeIndex(currentIndex + direction, maxLength);

        int thighIndex = NormalizeIndex(currentIndex, thighLength == 0 ? 1 : thighLength);
        int shinIndex = NormalizeIndex(currentIndex, shinLength == 0 ? 1 : shinLength);
        int waistIndex = NormalizeIndex(currentIndex, waistLength == 0 ? 1 : waistLength);
        waist_index = waistIndex;

        Debug.Log($"[OutfitManager] SwitchLegs -> dir {direction} newIndex {currentIndex} | thighIndex {thighIndex} | shinIndex {shinIndex} | waistIndex {waistIndex} | maxLen {maxLength}");

        SetActiveOnlyAll(left_thigh_outfits, thighIndex);
        SetActiveOnlyAll(right_thigh_outfits, thighIndex);

        SetActiveOnlyAll(left_shin_outfits, shinIndex);
        SetActiveOnlyAll(right_shin_outfits, shinIndex);

        SetActiveOnlyAll(waist_outfits, waistIndex);

        LogActiveStates("after switch thighs", left_thigh_outfits);
        LogActiveStates("after switch shins", left_shin_outfits);
        LogActiveStates("after switch waist", waist_outfits);
    }

    int NormalizeIndex(int index, int maxLength)
    {
        if (maxLength <= 0)
        {
            return 0;
        }

        if (index < 0 || index >= maxLength)
        {
            index = ((index % maxLength) + maxLength) % maxLength;
        }

        return index;
    }

    void LogActiveStates(string label, GameObject[] outfits)
    {
        if (outfits == null)
        {
            Debug.Log($"[OutfitManager] {label}: none");
            return;
        }

        List<string> states = new List<string>();
        for (int i = 0; i < outfits.Length; i++)
        {
            var go = outfits[i];
            if (go == null)
            {
                states.Add($"{i}: null");
            }
            else
            {
                states.Add($"{i}: {go.name} active={go.activeSelf}");
            }
        }

        Debug.Log($"[OutfitManager] {label}: {string.Join(" | ", states)}");
    }

    GameObject[] EnsureOutfits(ref GameObject[] outfits, GameObject wrapper, string label)
    {
        if (outfits == null || outfits.Length == 0)
        {
            outfits = CollectChildOutfits(wrapper);
            Debug.Log($"[OutfitManager] EnsureOutfits ({label}) -> collected {outfits.Length} from {(wrapper == null ? "null" : wrapper.name)}");
        }

        return outfits;
    }

    void EnsurePairedOutfits(ref GameObject[] left, GameObject leftWrapper, ref GameObject[] right, GameObject rightWrapper, string label)
    {
        EnsureOutfits(ref left, leftWrapper, $"{label}-left");
        EnsureOutfits(ref right, rightWrapper, $"{label}-right");
    }

    int ResolvePairLength(int firstLength, int secondLength)
    {
        if (firstLength == 0 && secondLength == 0)
        {
            return 0;
        }

        if (firstLength == 0)
        {
            return secondLength;
        }

        if (secondLength == 0)
        {
            return firstLength;
        }

        return Mathf.Min(firstLength, secondLength);
    }

    void AlignShinsToLegs()
    {
        int thighLength = MaxUsableLength(left_thigh_outfits, right_thigh_outfits);
        int shinLength = MaxUsableLength(left_shin_outfits, right_shin_outfits);
        int maxLength = ResolvePairLength(thighLength, shinLength);

        if (maxLength == 0)
        {
            return;
        }

        leg_index = NormalizeIndex(leg_index, maxLength);

        int shinIndex = NormalizeIndex(leg_index, shinLength == 0 ? 1 : shinLength);

        SetActiveOnlyAll(left_shin_outfits, shinIndex);
        SetActiveOnlyAll(right_shin_outfits, shinIndex);
    }

    void AlignWaistToLegs()
    {
        int thighLength = MaxUsableLength(left_thigh_outfits, right_thigh_outfits);
        int waistLength = MaxUsableLength(waist_outfits, null);
        int maxLength = ResolvePairLength(thighLength, waistLength);

        if (maxLength == 0)
        {
            return;
        }

        leg_index = NormalizeIndex(leg_index, maxLength);
        int waistIndex = NormalizeIndex(leg_index, waistLength == 0 ? 1 : waistLength);
        waist_index = waistIndex;

        SetActiveOnlyAll(waist_outfits, waistIndex);
    }

    void AlignArmsToBody()
    {
        int bodyLength = MaxUsableLength(body_outfits, null);
        int armLength = MaxUsableLength(left_arm_outfits, right_arm_outfits);
        int maxLength = ResolvePairLength(bodyLength, armLength);

        if (maxLength == 0)
        {
            return;
        }

        body_index = NormalizeIndex(body_index, maxLength);
        int armIndex = NormalizeIndex(body_index, armLength == 0 ? 1 : armLength);
        arm_index = armIndex;

        SetActiveOnlyAll(left_arm_outfits, arm_index);
        SetActiveOnlyAll(right_arm_outfits, arm_index);
    }

    string BuildIndexName(GameObject[] outfits, int index)
    {
        if (outfits == null)
        {
            return "none";
        }

        if (index < 0 || index >= outfits.Length)
        {
            return $"index {index} out of range (len {outfits.Length})";
        }

        return outfits[index] == null ? "null" : outfits[index].name;
    }

    void SetActive(GameObject[] outfits, int index, bool isActive)
    {
        if (outfits == null || index < 0 || index >= outfits.Length)
        {
            return;
        }

        GameObject outfit = outfits[index];
        if (outfit != null)
        {
            outfit.SetActive(isActive);
        }
    }
}
