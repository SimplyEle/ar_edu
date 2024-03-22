using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;
using TMPro;

public class ListItem : MonoBehaviour
{
    [SerializeField] TMP_Text textName;
    [SerializeField] TMP_Text textUsersCount;

    public void SetInfo(RoomInfo info)
    {
        textName.text = info.Name;
        textUsersCount.text = info.PlayerCount + "/" + info.MaxPlayers;
    }

    public void JoinToListRoom()
    {
        PhotonNetwork.JoinRoom(textName.text);
    }
}
