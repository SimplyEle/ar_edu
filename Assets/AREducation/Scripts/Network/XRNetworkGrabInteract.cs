using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Photon.Pun;

public class XRNetworkGrabInteract : XRGrabInteractable
{
    private PhotonView m_View;

    void Start()
    {
        m_View = GetComponent<PhotonView>();
    }

    void Update()
    {

    }

    protected override void OnSelectEntered(XRBaseInteractor interactor)
    {
        m_View.RequestOwnership();
        base.OnSelectEntered(interactor);
    }
}