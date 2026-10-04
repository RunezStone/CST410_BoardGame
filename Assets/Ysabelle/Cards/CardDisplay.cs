using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    [Header("Card Information")]
    public TMP_Text CardName;
    public TMP_Text CardDescription;

    [Header("Points")]
    [SerializeField] Image point1;
    [SerializeField] Image point2;
    [SerializeField] Image point3;

    [Header("Button")]
    public Button myButton;

    public void ChangeDisplay(string name, string description, int numOfPoints)
    {
        CardName.text = name;
        CardDescription.text = description;

        point1.gameObject.SetActive(false);
        point2.gameObject.SetActive(false);
        point3.gameObject.SetActive(false);

        if (numOfPoints >= 1)
        {
            point1.gameObject.SetActive(true);
            if (numOfPoints >= 2)
            {
                point2.gameObject.SetActive(true);
                if (numOfPoints >= 3)
                {
                    point3.gameObject.SetActive(true);
                }
            }
        }
    }
}