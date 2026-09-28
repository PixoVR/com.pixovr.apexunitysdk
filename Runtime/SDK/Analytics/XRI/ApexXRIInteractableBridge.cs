#if APEX_XRI
using PixoVR.Apex.Analytics;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace PixoVR.Apex.Analytics.XRI
{
    [RequireComponent(typeof(XRBaseInteractable), typeof(ApexInteractable))]
    public sealed class ApexXRIInteractableBridge : MonoBehaviour
    {
        private XRBaseInteractable interactable;
        private ApexInteractable apexInteractable;

        private void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();
            apexInteractable = GetComponent<ApexInteractable>();
        }

        private void OnEnable()
        {
            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.selectExited.AddListener(OnSelectExited);
            interactable.hoverEntered.AddListener(OnHoverEntered);
            interactable.hoverExited.AddListener(OnHoverExited);
            interactable.activated.AddListener(OnActivated);
        }

        private void OnDisable()
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.selectExited.RemoveListener(OnSelectExited);
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
            interactable.activated.RemoveListener(OnActivated);
        }

        private void OnSelectEntered(SelectEnterEventArgs arguments)
        {
            apexInteractable.Grab();
        }

        private void OnSelectExited(SelectExitEventArgs arguments)
        {
            apexInteractable.Release();
        }

        private void OnHoverEntered(HoverEnterEventArgs arguments)
        {
            apexInteractable.HoverEnter();
        }

        private void OnHoverExited(HoverExitEventArgs arguments)
        {
            apexInteractable.HoverExit();
        }

        private void OnActivated(ActivateEventArgs arguments)
        {
            apexInteractable.Use();
        }
    }
}
#endif
