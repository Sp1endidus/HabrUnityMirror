using Mirror;
using System.Collections.Generic;
using UnityEngine;

//mark the object as a network object by inheriting from NetworkBehaviour
//помечаем объект как сетевой, унаследовавшись от NetworkBehaviour
public class Player : NetworkBehaviour {
    //set the method to be called when the variable is synced
    //задаем метод, который будет выполняться при синхронизации переменной
    [SyncVar(hook = nameof(SyncHealth))]
    private int _syncHealth;
    [field: SerializeField]
    public int Health { get; private set; }

    [SerializeField]
    private GameObject[] healthGos;

    //in the case of SyncList you don't need to put a SyncVar and set a method, it'is done differently
    //в случае SyncList не нужно ставить SyncVar и задавать метод, это делается иначе
    private readonly SyncList<Vector3> _syncVector3Vars = new SyncList<Vector3>();
    private List<Vector3> _vector3Vars;

    [SerializeField]
    private GameObject pointPrefab;
    [SerializeField]
    private LineRenderer lineRenderer;
    [SerializeField]
    private GameObject bulletPrefab;

    private int _pointsCount;

    void Update() {
        //check the ownershop of the object
        //проверяем, есть ли у нас права изменять этот объект
        if (isOwned) {
            //make a simple movement
            //делаем простейшее движение
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            float speed = 5f * Time.deltaTime;
            transform.Translate(new Vector2(h * speed, v * speed));

            //take HP from self by pressing the H key
            //отнимаем у себя жизнь по нажатию клавиши H
            if (Input.GetKeyDown(KeyCode.H)) {
                //if we are the server, then go to the changing of the variable
                //если мы являемся сервером, то переходим к непосредственному изменению переменной
                if (isServer) {
                    ChangeHealthValue(Health - 1);
                } else {
                    //in other case, send a change request to the server
                    //в противном случае делаем на сервер запрос об изменении переменной
                    CmdChangeHealth(Health - 1);
                }
            }

            if (Input.GetKeyDown(KeyCode.P)) {
                if (isServer) {
                    ChangeVector3Vars(transform.position);
                } else {
                    CmdChangeVector3Vars(transform.position);
                }
            }

            if (Input.GetKeyDown(KeyCode.Mouse1)) {
                Vector3 pos = Input.mousePosition;
                pos.z = 10f;
                pos = Camera.main.ScreenToWorldPoint(pos);

                if (isServer) {
                    SpawnBullet(netId, pos);
                } else {
                    CmdSpawnBullet(netId, pos);
                }
            }
        }

        for (int i = 0; i < healthGos.Length; i++) {
            healthGos[i].SetActive(!(Health - 1 < i));
        }

        for (int i = _pointsCount; i < _vector3Vars.Count; i++) {
            Instantiate(pointPrefab, _vector3Vars[i], Quaternion.identity);
            _pointsCount++;

            lineRenderer.positionCount = _vector3Vars.Count;
            lineRenderer.SetPositions(_vector3Vars.ToArray());
        }
    }

    //sync hook always required two values - old and new
    //обязательно делаем два значения - старое и новое
    void SyncHealth(int oldValue, int newValue) {
        Health = newValue;
    }

    //mark the method for calling and executing only on the server
    //обозначаем, что этот метод будет вызываться и выполняться только на сервере
    [Server]
    public void ChangeHealthValue(int newValue) {
        _syncHealth = newValue;

        if (_syncHealth <= 0) {
            NetworkServer.Destroy(gameObject);
        }
    }

    //mark the method for calling on the client and executing on the server
    //обозначаем, что этот метод должен будет выполняться на сервере по запросу клиента
    [Command]
    //be sure to put Cmd at the beginning of the method name
    //обязательно ставим Cmd в начале названия метода
    public void CmdChangeHealth(int newValue) {
        //переходим к непосредственному изменению переменной
        ChangeHealthValue(newValue);
    }

    [Server]
    void ChangeVector3Vars(Vector3 newValue) {
        _syncVector3Vars.Add(newValue);
    }

    [Command]
    public void CmdChangeVector3Vars(Vector3 newValue) {
        ChangeVector3Vars(newValue);
    }

    void SyncVector3Vars(SyncList<Vector3>.Operation op, int index, Vector3 oldItem, Vector3 newItem) {
        switch (op) {
            case SyncList<Vector3>.Operation.OP_ADD: {
                    _vector3Vars.Add(newItem);
                    break;
                }
            case SyncList<Vector3>.Operation.OP_CLEAR:
                break;
            case SyncList<Vector3>.Operation.OP_INSERT:
                break;
            case SyncList<Vector3>.Operation.OP_REMOVEAT:
                break;
            case SyncList<Vector3>.Operation.OP_SET:
                break;
        }
    }

    public override void OnStartClient() {
        base.OnStartClient();

        //instead of hook, use subscription on Callback
        //вместо hook для SyncList используем подписку на Callback
        _syncVector3Vars.Callback += SyncVector3Vars;

        //because Callback only acts on array changes,
        //and we may already have some data in the array at the time of connection,
        //we need to put this data into the local array

        //так как Callback действует только на изменение массива,
        //а у нас на момент подключения уже могут быть какие-то данные в массиве,
        //нам нужно эти данные внести в локальный массив
        _vector3Vars = new List<Vector3>(_syncVector3Vars.Count);
        for (int i = 0; i < _syncVector3Vars.Count; i++) {
            _vector3Vars.Add(_syncVector3Vars[i]);
        }
    }

    [Server]
    public void SpawnBullet(uint owner, Vector3 target) {
        //create a local object of the bullet on the server
        //создаем локальный объект пули на сервере
        GameObject bulletGo = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        //send info about the network object to all players
        //отправляем информацию о сетевом объекте всем игрокам
        NetworkServer.Spawn(bulletGo);

        //init the bullet behaviour
        //инициализируем поведение пули
        bulletGo.GetComponent<Bullet>().Init(owner, target);
    }

    [Command]
    public void CmdSpawnBullet(uint owner, Vector3 target) {
        SpawnBullet(owner, target);
    }
}