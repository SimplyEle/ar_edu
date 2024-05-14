using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

        private int localUserId = PhotonNetwork.LocalPlayer.ActorNumber;

        bool m_firstTake = false;

        void OnEnable()
        {
            m_firstTake = true;
        }

        public void Initialize(int objectID, int photonViewID, int ownerId, Vector3 relativePosition)
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

            previousRotation = transform.rotation;

            var arPlane = FindObjectOfType<ARPlane>();
            previousPosition = arPlane.transform.TransformPoint(relativePosition);
            networkPosition = arPlane.transform.TransformPoint(relativePosition);
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
            if (ownerPhotonView != null && localUserId == ownerUserId)
            {
                transform.position = Vector3.MoveTowards(transform.position, networkPosition, distance * Time.deltaTime * PhotonNetwork.SerializationRate);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, networkRotation, angle * Time.deltaTime * PhotonNetwork.SerializationRate);
                thisPhotonView.RPC("UpdateRemoteObject", RpcTarget.OthersBuffered, networkRotation, ownerPhotonViewId);
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                direction = transform.position - previousPosition;
                previousPosition = transform.position;
                ownerUserId = PhotonNetwork.LocalPlayer.ActorNumber;
                stream.SendNext(transform.position);
                stream.SendNext(direction);
                stream.SendNext(transform.rotation);
                stream.SendNext(ownerUserId);
            }
            else
            {
                networkPosition = (Vector3)stream.ReceiveNext();
                networkPosition = new Vector3(networkPosition.x, transform.position.y, networkPosition.z);
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
                    distance = Vector3.Distance(transform.position, networkPosition);
                }
                networkRotation = (Quaternion)stream.ReceiveNext();
                ownerUserId = (int)stream.ReceiveNext();
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

            if (view != null && localUserId != ownerUserId)
            {
                var position = networkPosition;
                position.y = view.gameObject.transform.position.y;
                view.gameObject.transform.position = Vector3.MoveTowards(view.gameObject.transform.position, position, distance * Time.deltaTime * PhotonNetwork.SerializationRate);
                view.gameObject.transform.rotation = Quaternion.RotateTowards(view.gameObject.transform.rotation, rotation, angle * Time.deltaTime * PhotonNetwork.SerializationRate);
                view.gameObject.GetComponent<ObjectSyncController>().networkPosition = position;
            }
        }

        [PunRPC]
        private void DeleteOnAllClients(int viewId)
        {
            PhotonNetwork.Destroy(PhotonView.Find(MakePseudoViewId(viewId)));
        }
    }
}
