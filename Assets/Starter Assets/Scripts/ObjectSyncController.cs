using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    public class ObjectSyncController : MonoBehaviourPun, IPunObservable
    {
        private float distance;
        private float angle;

        private PhotonView thisPhotonView;
        private PhotonView ownerPhotonView;
        public int ownerPhotonViewId;
        public int thisPhotonViewId;
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

            if (thisPhotonView != null && ownerPhotonView != null)
            {
                thisPhotonView.ObservedComponents.Add(this);
                ownerPhotonView.ObservedComponents.Add(this);
                thisPhotonView.ObservedComponents.Add(PhotonView.Find(ownerPhotonViewId).GetComponent<ObjectSyncController>());
                thisPhotonView.RPC("AddObservable", RpcTarget.Others, ownerPhotonViewId, thisPhotonViewId);
            }

            ownerUserId = ownerId;

            previousRotation = transform.rotation;
            previousPosition = position;

            networkPosition = position;
            
        }

        [PunRPC]
        void AddObservable(int oPViewId, int tPViewId)
        {
            PhotonView.Find(oPViewId).ObservedComponents.Add(PhotonView.Find(tPViewId).GetComponent<ObjectSyncController>());
        }

        private void FixedUpdate()
        {
            if (photonView.Owner != PhotonNetwork.LocalPlayer)
            {
                gameObject.GetComponentInChildren<MeshRenderer>().enabled = false;
            }
            if (ownerPhotonView != null && photonView.IsMine)
            {
                transform.position = Vector3.MoveTowards(transform.position, networkPosition, distance * Time.deltaTime * PhotonNetwork.SerializationRate);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, networkRotation, angle * Time.deltaTime * PhotonNetwork.SerializationRate);
                thisPhotonView.RPC("UpdateRemoteObject", RpcTarget.AllViaServer, networkRotation, ownerPhotonViewId);
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

        private int MakePseudoViewId(int viewId)
        {
            int[] splitParts = new int[2];

            int playerID = viewId / 1000;
            int viewNumber = viewId % 1000;

            splitParts[0] = playerID;
            splitParts[1] = viewNumber;

            splitParts[0] = PhotonNetwork.LocalPlayer.ActorNumber;
            int res = int.Parse("" + splitParts[0] + new string('0', 3 - splitParts[1].ToString().Length) + splitParts[1]);

            return res;
        }

        [PunRPC]
        private void UpdateRemoteObject(Quaternion rotation, int ownerPViewId)
        {
            var view = PhotonView.Find(MakePseudoViewId(ownerPViewId));

            if (view != null)
            {
                var position = networkPosition;
                position.y = view.gameObject.transform.position.y;
                view.gameObject.transform.position = Vector3.Lerp(view.gameObject.transform.position, position, Time.deltaTime * 3f);
                view.gameObject.transform.rotation = Quaternion.RotateTowards(view.gameObject.transform.rotation, rotation, angle * Time.deltaTime * PhotonNetwork.SerializationRate);
            }
        }

        [PunRPC]
        private void DeleteOnAllClients(int viewId)
        {
            PhotonNetwork.Destroy(PhotonView.Find(MakePseudoViewId(viewId)));
        }
    }
}
