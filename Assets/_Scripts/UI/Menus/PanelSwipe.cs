using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class PanelSwipe : MonoBehaviour, IDragHandler, IEndDragHandler {

    
    public float percentThreshold = 0.2f; // Sensitivity of swipe detector. Smaller number = more sensitive
    public float easing = 0.5f; // Makes the transition less jarring
    public int currentScreen; // Keeps track of how many screens you have in the menu system. From 0 to 4, home = 2

    public GameObject Ship_Select;
    public GameObject Minigame_Settings;
    public GameObject Coming_Soon;
    
    [SerializeField] Transform NavBar;

    Vector3 panelLocation;
    Coroutine navigateCoroutine;

    const int OPTIONS = 0;
    const int RECORDS = 1;
    const int HOME = 2;
    const int HANGAR = 3;
    const int ARCADE = 4;
    
    void Start()
    {
        NavigateTo(HOME, false);
    }

    public void OnDrag(PointerEventData data)
    {
        transform.position = panelLocation - new Vector3(data.pressPosition.x - data.position.x, 0, 0);
    }

    public void OnEndDrag(PointerEventData data)
    {
        float percentage = (data.pressPosition.x - data.position.x) / Screen.width;

        if (percentage >= percentThreshold && currentScreen < transform.childCount - 1)
            NavigateRight();
        else if (percentage <= -percentThreshold && currentScreen > 0)
            NavigateLeft();
        else
        {
            // Reset back to current screen
            if (navigateCoroutine != null)
                StopCoroutine(navigateCoroutine);
            navigateCoroutine = StartCoroutine(SmoothMove(transform.position, panelLocation, easing));
        }
    }

    public void NavigateTo(int ScreenIndex, bool animate=true) {
        ScreenIndex = Mathf.Clamp(ScreenIndex, 0, transform.childCount - 1);

        if (ScreenIndex == currentScreen)
            return;

        Vector3 newLocation = new Vector3(-ScreenIndex * Screen.width, 0, 0);
        panelLocation = newLocation;

        if (animate)
        {
            if (navigateCoroutine != null)
                StopCoroutine(navigateCoroutine);
            navigateCoroutine = StartCoroutine(SmoothMove(transform.position, newLocation, easing));
        }
        else
            transform.position = newLocation;

        currentScreen = ScreenIndex;
        UpdateNavBar(currentScreen);
        DeactiveSubpages();
    }

    public void OnClickOptionsMenuButton()
    {
        NavigateTo(OPTIONS);
    }
    public void OnClickRecords()
    {
        NavigateTo(RECORDS);
    }
    public void OnClickHome()
    {
        NavigateTo(HOME);
    }
    public void OnClickHangar()
    {
        NavigateTo(HANGAR);
    }
    public void OnClickMinigames()
    {
        NavigateTo(ARCADE);
    }
    public void NavigateLeft()
    {
        if (currentScreen <= 0)
            return;

        NavigateTo(currentScreen - 1);
    }
    public void NavigateRight()
    {
        if (currentScreen >= transform.childCount - 1)
            return;

        NavigateTo(currentScreen + 1);
    }
    void DeactiveSubpages()
    {
        Ship_Select.SetActive(false);
        Minigame_Settings.SetActive(false);
        Coming_Soon.SetActive(false);
    }
    void UpdateNavBar(int index)
    {
        // Deselect them all
        for (var i = 1; i < NavBar.childCount-1; i++)
        {
            NavBar.GetChild(i).GetChild(0).gameObject.SetActive(true);
            NavBar.GetChild(i).GetChild(1).gameObject.SetActive(false);
        }

        // Select the one
        NavBar.GetChild(index+1).GetChild(0).gameObject.SetActive(false);
        NavBar.GetChild(index+1).GetChild(1).gameObject.SetActive(true);
    }
    IEnumerator SmoothMove(Vector3 startpos, Vector3 endpos, float seconds)
    {
        float t = 0f;
        while (t <= 1.0)
        {
            t += Time.deltaTime / seconds;
            transform.position = Vector3.Lerp(startpos, endpos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
    }
}