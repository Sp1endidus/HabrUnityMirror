using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : NetworkBehaviour //даем системе понять, что это сетевой объект
{
    [SyncVar(hook = nameof(SyncHealth))] //задаем метод, который будет выполняться при синхронизации переменной
    int _SyncHealth;
    public int Health;
    public GameObject[] HealthGos;


    SyncList<Vector3> _SyncVector3Vars = new SyncList<Vector3>(); //В случае SyncList не нужно ставить SyncVar и задавать метод, это делается иначе
    public List<Vector3> Vector3Vars;


    public GameObject PointPrefab; //сюда вешаем ранее созданные префаб Point
    public LineRenderer LineRenderer; //сюда кидаем наш же компонент
    int pointsCount;

    public GameObject BulletPrefab; //сюда вешаем префаб пули

    void Update()
    {
        if (hasAuthority) //проверяем, есть ли у нас права изменять этот объект
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            float speed = 5f * Time.deltaTime;
            transform.Translate(new Vector2(h * speed, v * speed)); //делаем простейшее движение

            if (Input.GetKeyDown(KeyCode.H)) //отнимаем у себя жизнь по нажатию клавиши H
            {
                if (isServer) //если мы являемся сервером, то переходим к непосредственному изменению переменной
                    ChangeHealthValue(Health - 1);
                else
                    CmdChangeHealth(Health - 1); //в противном случае делаем на сервер запрос об изменении переменной
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                if (isServer)
                    ChangeVector3Vars(transform.position);
                else
                    CmdChangeVector3Vars(transform.position);
            }

            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                Vector3 pos = Input.mousePosition;
                pos.z = 10f;
                pos = Camera.main.ScreenToWorldPoint(pos);

                if (isServer)
                    SpawnBullet(netId, pos);
                else
                    CmdSpawnBullet(netId, pos);
            }
        }

        for (int i = 0; i < HealthGos.Length; i++)
        {
            HealthGos[i].SetActive(!(Health - 1 < i));
        }

        for (int i = pointsCount; i < Vector3Vars.Count; i++)
        {
            Instantiate(PointPrefab, Vector3Vars[i], Quaternion.identity);
            pointsCount++;

            LineRenderer.positionCount = Vector3Vars.Count;
            LineRenderer.SetPositions(Vector3Vars.ToArray());
        }
    }

    void SyncHealth(int oldValue, int newValue) //обязательно делаем два значения - старое и новое. 
    {
        Health = newValue;
    }

    [Server] //обозначаем, что этот метод будет вызываться и выполняться только на сервере
    public void ChangeHealthValue(int newValue)
    {
        _SyncHealth = newValue;

        if (_SyncHealth <= 0)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    [Command] //обозначаем, что этот метод должен будет выполняться на сервере по запросу клиента
    public void CmdChangeHealth(int newValue) //обязательно ставим Cmd в начале названия метода
    {
        ChangeHealthValue(newValue); //переходим к непосредственному изменению переменной
    }


    [Server]
    void ChangeVector3Vars(Vector3 newValue)
    {
        _SyncVector3Vars.Add(newValue);
    }

    [Command]
    public void CmdChangeVector3Vars(Vector3 newValue)
    {
        ChangeVector3Vars(newValue);
    }

    void SyncVector3Vars(SyncList<Vector3>.Operation op, int index, Vector3 oldItem, Vector3 newItem)
    {
        switch (op)
        {
            case SyncList<Vector3>.Operation.OP_ADD:
                {
                    Vector3Vars.Add(newItem);
                    break;
                }
            case SyncList<Vector3>.Operation.OP_CLEAR:
                {

                    break;
                }
            case SyncList<Vector3>.Operation.OP_INSERT:
                {

                    break;
                }
            case SyncList<Vector3>.Operation.OP_REMOVEAT:
                {

                    break;
                }
            case SyncList<Vector3>.Operation.OP_SET:
                {

                    break;
                }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        _SyncVector3Vars.Callback += SyncVector3Vars; //вместо hook для SyncList используем подписку на Callback

        Vector3Vars = new List<Vector3>(_SyncVector3Vars.Count); //так как Callback действует только на изменение массива,  
        for (int i = 0; i < _SyncVector3Vars.Count; i++) //а у нас на момент подключения уже могут быть какие-то данные в массиве, нам нужно эти данные внести в локальный массив
        {
            Vector3Vars.Add(_SyncVector3Vars[i]);
        }
    }

    [Server]
    public void SpawnBullet(uint owner, Vector3 target)
    {
        GameObject bulletGo = Instantiate(BulletPrefab, transform.position, Quaternion.identity); //Создаем локальный объект пули на сервере
        NetworkServer.Spawn(bulletGo); //отправляем информацию о сетевом объекте всем игрокам.
        bulletGo.GetComponent<Bullet>().Init(owner, target); //инифиализируем поведение пули
    }


    [Command]
    public void CmdSpawnBullet(uint owner, Vector3 target)
    {
        SpawnBullet(owner, target);
    }
}