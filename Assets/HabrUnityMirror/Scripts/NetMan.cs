using Mirror;
using UnityEngine;

//inherit from NetworkManager to extend its functionality
//наследуемся от NetworkManager, чтобы дополнить его функционал
public class NetMan : NetworkManager {
    private bool _playerSpawned;
    private bool _playerConnected;

    public void OnCreateCharacter(NetworkConnectionToClient conn, PosMessage message) {
        //create a gameObject locally on the server
        //локально на сервере создаем gameObject
        GameObject go = Instantiate(playerPrefab, message.vector2, Quaternion.identity);

        //attach gameObject to the network object pool and send information about it to the other players
        //присоеднияем gameObject к пулу сетевых объектов и отправляем информацию об этом остальным игрокам
        NetworkServer.AddPlayerForConnection(conn, go);
    }

    public override void OnStartServer() {
        base.OnStartServer();

        //specify which struct must come to the server for the object creation
        //указываем, какой struct должен прийти на сервер, чтобы выполнилось создание объекта
        NetworkServer.RegisterHandler<PosMessage>(OnCreateCharacter);
    }

    public void ActivatePlayerSpawn() {
        Vector3 pos = Input.mousePosition;
        pos.z = 10f;
        pos = Camera.main.ScreenToWorldPoint(pos);

        //create struct of a certain type, so that the server understands what this data refers to
        //создаем struct определенного типа, чтобы сервер понял к чему эти данные относятся
        PosMessage m = new PosMessage() {
            vector2 = pos
        };


        //send a message to the server with the coordinates of the object creation
        //отправляем сообщение на сервер с координатами создания объекта
        NetworkClient.Send(m);
        _playerSpawned = true;
    }

    public override void OnClientConnect() {
        base.OnClientConnect();
        _playerConnected = true;
    }

    public override void Update() {
        base.Update();

        if (Input.GetKeyDown(KeyCode.Mouse0) && !_playerSpawned && _playerConnected) {
            ActivatePlayerSpawn();
        }
    }
}

//inherit from the NetworkMessage interface, so that the system understands what data to pack
//наследуемся от интерфейса NetworkMessage, чтобы система поняла какие данные упаковывать
public struct PosMessage : NetworkMessage {
    //you can't use a property
    //нельзя использовать property
    public Vector2 vector2;
}