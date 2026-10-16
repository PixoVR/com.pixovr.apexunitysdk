#if APEX_XRI
using PixoVR.Apex.Analytics;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace PixoVR.Apex.Analytics.XRI
{
	[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable),
		typeof(ApexInteractable))]
	public sealed class ApexXRIInteractableBridge : MonoBehaviour
	{
		private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable m_interactable;
		private ApexInteractable m_apexInteractable;

		private void Awake()
		{
			m_interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
			m_apexInteractable = GetComponent<ApexInteractable>();
		}

		private void OnEnable()
		{
			m_interactable.selectEntered.AddListener(OnSelectEntered);
			m_interactable.selectExited.AddListener(OnSelectExited);
			m_interactable.hoverEntered.AddListener(OnHoverEntered);
			m_interactable.hoverExited.AddListener(OnHoverExited);
			m_interactable.activated.AddListener(OnActivated);
		}

		private void OnDisable()
		{
			m_interactable.selectEntered.RemoveListener(OnSelectEntered);
			m_interactable.selectExited.RemoveListener(OnSelectExited);
			m_interactable.hoverEntered.RemoveListener(OnHoverEntered);
			m_interactable.hoverExited.RemoveListener(OnHoverExited);
			m_interactable.activated.RemoveListener(OnActivated);
		}

		private void OnSelectEntered(SelectEnterEventArgs arguments)
		{
			m_apexInteractable.Grab();
		}

		private void OnSelectExited(SelectExitEventArgs arguments)
		{
			m_apexInteractable.Release();
		}

		private void OnHoverEntered(HoverEnterEventArgs arguments)
		{
			m_apexInteractable.HoverEnter();
		}

		private void OnHoverExited(HoverExitEventArgs arguments)
		{
			m_apexInteractable.HoverExit();
		}

		private void OnActivated(ActivateEventArgs arguments)
		{
			m_apexInteractable.Use();
		}
	}
}
#endif
