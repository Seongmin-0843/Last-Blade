using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 레벨업 카드 2장. 탭·클릭 또는 1/2 키로 고른다.
/// </summary>
public class LevelUpUI : MonoBehaviour
{
    [Serializable]
    class Card
    {
        public Button button;
        public Text title;
        public Text level;
        public Text description;
    }

    [SerializeField] GameObject panel;
    [SerializeField] Card[] cards;

    readonly AbilityData[] shown = new AbilityData[2];

    Action<AbilityData> chosen;

    void Awake()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            int index = i;
            cards[i].button.onClick.AddListener(() => Choose(index));
        }

        panel.SetActive(false);
    }

    public void Show(AbilityData first, int firstLevel, AbilityData second, int secondLevel, Action<AbilityData> onChosen)
    {
        chosen = onChosen;
        Fill(0, first, firstLevel);
        Fill(1, second, secondLevel);
        panel.SetActive(true);
    }

    void Fill(int index, AbilityData ability, int level)
    {
        shown[index] = ability;
        cards[index].title.text = ability.displayName;
        cards[index].level.text = level == 0 ? "NEW" : $"{level}단계 → {level + 1}단계";
        cards[index].description.text = ability.descriptions[level];
    }

    void Update()
    {
        if (!panel.activeSelf || Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) Choose(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) Choose(1);
    }

    void Choose(int index)
    {
        panel.SetActive(false);
        chosen(shown[index]);
    }
}
