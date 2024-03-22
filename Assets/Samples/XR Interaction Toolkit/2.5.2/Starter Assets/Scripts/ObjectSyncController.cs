using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;

public class ObjectSyncController : MonoBehaviourPun, IPunObservable
{
    private float distance;
    private float angle;

    private PhotonView thisPhotonView;
    private PhotonView ownerPhotonView;
    [SerializeField]
    private int ownerPhotonViewId;
    [SerializeField]
    private int thisPhotonViewId;
    private int ownerUserId;
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Quaternion previousRotation;
    private Vector3 previousPosition;
    private Vector3 direction;

    bool m_firstTake = false;

    void OnEnable()
    {
        m_firstTake = true;
    }

    public void Initialize(int objectID, int photonViewID, int ownerId, Vector3 position)
    {
        thisPhotonViewId = objectID;
        ownerPhotonViewId = photonViewID;

        thisPhotonView = PhotonView.Find(objectID).GetComponent<PhotonView>();
        ownerPhotonView = PhotonView.Find(photonViewID).GetComponent<PhotonView>();

        ownerUserId = ownerId;

        previousRotation = transform.rotation;
        previousPosition = position;

        networkPosition = position;

        if (ownerPhotonView != null)
        {
            thisPhotonView.ObservedComponents.Add(this);
            ownerPhotonView.ObservedComponents.Add(this);
        }
    }

    private void Update()
    {
        if (ownerPhotonView != null && !ownerPhotonView.IsMine)
        {
            ownerPhotonView.RPC("UpdateRemoteObject", RpcTarget.AllBufferedViaServer, networkPosition, networkRotation, thisPhotonViewId, ownerPhotonViewId, ownerUserId);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            direction = transform.position - previousPosition;
            previousPosition = transform.position;
            stream.SendNext(transform.position);
            stream.SendNext(direction);
            stream.SendNext(transform.rotation);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkPosition.Set(networkPosition.x, transform.position.y, networkPosition.z);
            direction = (Vector3)stream.ReceiveNext();
            if (m_firstTake)
            {
                transform.position.Set(networkPosition.x, transform.position.y, networkPosition.z);
                distance = 0f;
            }
            else
            {
                float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
                networkPosition += direction * lag;
                var tmpNetworkPosition = Vector3.zero;
                tmpNetworkPosition.Set(networkPosition.x, transform.position.y, networkPosition.z);
                distance = Vector3.Distance(transform.position, tmpNetworkPosition);
            }
            networkRotation = (Quaternion)stream.ReceiveNext();
            if (m_firstTake)
            {
                angle = 0f;
                transform.rotation = networkRotation;
            }
            else
            {
                angle = Quaternion.Angle(transform.rotation, networkRotation);
            }
            if (m_firstTake)
            {
                m_firstTake = false;
            }
        }
    }

    [PunRPC]
    private void UpdateRemoteObject(Vector3 position, Quaternion rotation, int thisPhotonViewId, int ownerPhotonViewId, int ownerId)
    {        
        if (PhotonView.Find(ownerPhotonViewId) != null && ownerId != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            PhotonView.Find(thisPhotonViewId).gameObject.transform.position = Vector3.MoveTowards(PhotonView.Find(thisPhotonViewId).gameObject.transform.position, position, distance * Time.deltaTime * PhotonNetwork.SerializationRate);
            PhotonView.Find(thisPhotonViewId).gameObject.transform.rotation = Quaternion.RotateTowards(PhotonView.Find(thisPhotonViewId).gameObject.transform.rotation, rotation, angle * Time.deltaTime * PhotonNetwork.SerializationRate);
        }
    }

    /*private float m_Distance;
    private float m_Angle;

    private Vector3 m_Direction;
    private Vector3 m_NetworkPosition;
    private Vector3 m_StoredPosition;

    private Quaternion m_NetworkRotation;

    public bool m_SynchronizePosition = true;
    public bool m_SynchronizeRotation = true;
    public bool m_SynchronizeScale = false;

    private PhotonView thisPhotonView;
    private PhotonView ownerPhotonView;
    private int ownerPhotonViewId;
    private int thisPhotonViewId;
    private int ownerUserId;

    public bool m_UseLocal;

    bool m_firstTake = false;

    public void Awake()
    {
        m_StoredPosition = transform.localPosition;
        m_NetworkPosition = Vector3.zero;
        m_NetworkRotation = Quaternion.identity;
    }

    public void Initialize(int objectID, int photonViewID, int ownerId)
    {
        thisPhotonViewId = objectID;
        ownerPhotonViewId = photonViewID;
        thisPhotonView = PhotonView.Find(objectID).GetComponent<PhotonView>();
        ownerPhotonView = PhotonView.Find(photonViewID).GetComponent<PhotonView>();
        Debug.Log(thisPhotonView);
        ownerUserId = ownerId;
        if (ownerPhotonView != null)
        {
            thisPhotonView.ObservedComponents.Add(this);
            ownerPhotonView.ObservedComponents.Add(this);
        }
    }

    private void Reset()
    {
        m_UseLocal = true;
    }

    void OnEnable()
    {
        m_firstTake = true;
    }

    public void Update()
    {
        var tr = transform;

        if (!ownerPhotonView.IsMine)
        {
            ownerPhotonView.RPC("UpdateRemoteObject", RpcTarget.AllBuffered, m_NetworkPosition, m_NetworkRotation, thisPhotonViewId, ownerPhotonViewId, ownerUserId);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        var tr = transform;

        // Write
        if (stream.IsWriting)
        {
            if (m_SynchronizePosition)
            {
                if (m_UseLocal)
                {
                    m_Direction = tr.localPosition - m_StoredPosition;
                    m_StoredPosition = tr.localPosition;
                    stream.SendNext(tr.localPosition);
                    stream.SendNext(m_Direction);
                }
                else
                {
                    m_Direction = tr.position - m_StoredPosition;
                    m_StoredPosition = tr.position;
                    stream.SendNext(tr.position);
                    stream.SendNext(m_Direction);
                }
            }

            if (m_SynchronizeRotation)
            {
                if (m_UseLocal)
                {
                    stream.SendNext(tr.localRotation);
                }
                else
                {
                    stream.SendNext(tr.rotation);
                }
            }

            if (m_SynchronizeScale)
            {
                stream.SendNext(tr.localScale);
            }
        }
        // Read
        else
        {
            if (m_SynchronizePosition)
            {
                m_NetworkPosition = (Vector3)stream.ReceiveNext();
                m_Direction = (Vector3)stream.ReceiveNext();

                if (m_firstTake)
                {
                    if (m_UseLocal)
                        tr.localPosition = m_NetworkPosition;
                    else
                        tr.position = m_NetworkPosition;

                    m_Distance = 0f;
                }
                else
                {
                    float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
                    m_NetworkPosition += m_Direction * lag;
                    if (m_UseLocal)
                    {
                        m_Distance = Vector3.Distance(tr.localPosition, m_NetworkPosition);
                    }
                    else
                    {
                        m_Distance = Vector3.Distance(tr.position, m_NetworkPosition);
                    }
                }

            }

            if (m_SynchronizeRotation)
            {
                m_NetworkRotation = (Quaternion)stream.ReceiveNext();

                if (m_firstTake)
                {
                    m_Angle = 0f;

                    if (m_UseLocal)
                    {
                        tr.localRotation = m_NetworkRotation;
                    }
                    else
                    {
                        tr.rotation = m_NetworkRotation;
                    }
                }
                else
                {
                    if (m_UseLocal)
                    {
                        m_Angle = Quaternion.Angle(tr.localRotation, m_NetworkRotation);
                    }
                    else
                    {
                        m_Angle = Quaternion.Angle(tr.rotation, m_NetworkRotation);
                    }
                }
            }

            if (m_SynchronizeScale)
            {
                tr.localScale = (Vector3)stream.ReceiveNext();
            }

            if (m_firstTake)
            {
                m_firstTake = false;
            }
        }
    }

    [PunRPC]
    private void UpdateRemoteObject(Vector3 position, Quaternion rotation, int thisPhotonViewId, int ownerPhotonViewId, int ownerUserId)
    {
        //Debug.Log("this: " + thisPhotonViewId);
        //Debug.Log("ownr: " + ownerPhotonViewId);
        //Debug.Log("pv.f(ow): " + PhotonView.Find(ownerPhotonViewId));
        //Debug.Log("pv.f(th): " + PhotonView.Find(thisPhotonViewId));
        //Debug.Log("ow.ismin: " + PhotonView.Find(ownerPhotonViewId).IsMine);
        //Debug.Log("th.ismin: " + PhotonView.Find(thisPhotonViewId).IsMine);
        //Debug.Log("ownerUserId: " + ownerUserId);
        if (PhotonView.Find(ownerPhotonViewId) != null && PhotonNetwork.LocalPlayer.ActorNumber != ownerUserId)
        {
            //PhotonView.Find(thisPhotonViewId).transform.SetPositionAndRotation(position, rotation);
            transform.position = Vector3.MoveTowards(transform.position, position, m_Distance * Time.deltaTime * PhotonNetwork.SerializationRate);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, m_Angle * Time.deltaTime * PhotonNetwork.SerializationRate);
        }
    }*/
}

//public class ObjectSyncController : MonoBehaviourPunCallbacks, IPunObservable
//{
//    private PhotonView thisPhotonView;
//    private PhotonView ownerPhotonView;
//    private int ownerPhotonViewId;
//    private int thisPhotonViewId;
//    private Vector3 networkPosition;
//    private Quaternion networkRotation;
//    private Quaternion previousRotation;
//    private Vector3 previousPosition;

//    private bool hasRotationChanged;
//    private bool hasPositionChanged;

//    private ARPlane arPlane;

//    private void Awake()
//    {
//        arPlane = FindObjectOfType<ARPlane>();
//    }

//    public void Initialize(int objectID, int photonViewID)
//    {
//        thisPhotonViewId = objectID;
//        ownerPhotonViewId = photonViewID;
//        thisPhotonView = PhotonView.Find(objectID).GetComponent<PhotonView>();
//        ownerPhotonView = PhotonView.Find(photonViewID).GetComponent<PhotonView>();
//        previousRotation = transform.rotation;
//        previousPosition = arPlane.transform.InverseTransformPoint(transform.position);

//        if (ownerPhotonView != null)
//        {
//            thisPhotonView.ObservedComponents.Add(this);
//            ownerPhotonView.ObservedComponents.Add(this);
//        }
//    }

//    private void Update()
//    {
//        if (ownerPhotonView != null && ownerPhotonView.IsMine)
//        {
//            if (hasPositionChanged || hasRotationChanged)
//            {
//                ownerPhotonView.RPC("UpdateRemoteObject", RpcTarget.AllBuffered, networkPosition, networkRotation, thisPhotonViewId, ownerPhotonViewId);
//                hasPositionChanged = false;
//                hasRotationChanged = false;
//            }
//        }
//    }

//    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
//    {
//        if (stream.IsWriting)
//        {
//            stream.SendNext(hasPositionChanged);
//            if (hasPositionChanged)
//                stream.SendNext(arPlane.transform.InverseTransformPoint(transform.position));

//            stream.SendNext(hasRotationChanged);
//            if (hasRotationChanged)
//                stream.SendNext(transform.rotation);
//        }
//        else
//        {
//            hasPositionChanged = (bool)stream.ReceiveNext();
//            if (hasPositionChanged)
//                networkPosition = (Vector3)stream.ReceiveNext();

//            hasRotationChanged = (bool)stream.ReceiveNext();
//            if (hasRotationChanged)
//                networkRotation = (Quaternion)stream.ReceiveNext();
//        }
//    }

//    [PunRPC]
//    private void UpdateRemoteObject(Vector3 position, Quaternion rotation, int thisPhotonViewId, int ownerPhotonViewId)
//    {
//        if (PhotonView.Find(ownerPhotonViewId) != null && !PhotonView.Find(ownerPhotonViewId).IsMine &&
//            PhotonView.Find(thisPhotonViewId) != null && PhotonView.Find(thisPhotonViewId).IsMine)
//        {
//            transform.SetPositionAndRotation(position, rotation);
//        }
//    }

//    public override void OnMasterClientSwitched(Player newMasterClient)
//    {
//        if (ownerPhotonView != null && ownerPhotonView.IsMine)
//        {
//            hasPositionChanged = true;
//            hasRotationChanged = true;
//        }
//    }
//}
