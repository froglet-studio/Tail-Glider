using CosmicShore.Core;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mono.CSharp;

namespace CosmicShore
{
    public class ClassCard : MonoBehaviour //, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        //TODO OnEnable register for selected event or find where event is happening
        //TODO get locked from store or playfab

        [Header("Resources")]
        [SerializeField] SO_ShipList allShipClasses; // Not sure I want this
        [SerializeField] SO_Ship ship_SO;
        [SerializeField] Sprite ActiveLockIcon;
        [SerializeField] Sprite InactiveLockIcon;
        [SerializeField] Sprite ActiveBackgroundIcon;
        [SerializeField] Sprite InactiveBackgroundIcon;

        [Header("Placeholder Locations")]
        [SerializeField] TMP_Text ClassTitle;
        [SerializeField] Image BackgroundImage;
        [SerializeField] Image ShipImage;
        [SerializeField] Image LockImage;

        [SerializeField] int Index;
        [SerializeField] bool CardSelected;
        [SerializeField] bool locked;
        //Drag and Drop
       /* private RectTransform cardRectTransform;
        private CanvasGroup canvasGroup;
        private Vector2 offset;*/

        Ship ship;

        public Ship CurrentShip
        {
            get { return ship; }
            set
            {
                value = ship;
                UpdateCardView();
            }
        }

        


        // Start is called before the first frame update
        void Start()
        {
            /*cardRectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();*/
            
            UpdateCardView ();
            
        }

        void UpdateCardView()
        {
            if(ship_SO == null)
            {
                ship_SO = allShipClasses.ShipList[Index];
            }
            else
            {
                //set title
                ClassTitle.text = ship_SO.Name;
                //set background image
                if (CardSelected)
                {
                    BackgroundImage.sprite = ship_SO.ActiveIcon;
                }
                else
                {
                    BackgroundImage.sprite = ship_SO.InactiveIcon;
                }
                //set ship image
                ShipImage.sprite = ship_SO.ShipIcon;
            }
            //set Locks image
            if (locked) { LockImage.sprite = ActiveLockIcon; }
            else { LockImage.sprite = InactiveLockIcon; }
        }

       /* public void OnBeginDrag(PointerEventData eventData)
        {
            offset = eventData.position - (Vector2)cardRectTransform.position;
            canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            cardRectTransform.position = eventData.position - offset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            //TODO Find nearest ShipCardSlot
            //TODO If closest is within maxRange then switch cardRectTransforms else dropmove fails
            //Snap to nearest ShipCardSlot
            canvasGroup.blocksRaycasts = true;
        }*/
    }
}
