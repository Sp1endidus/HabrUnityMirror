using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    uint owner;
    bool inited;
    Vector3 target;

    [Server]
    public void Init(uint owner, Vector3 target)
    {
        this.owner = owner; //кто сделал выстрел
        this.target = target; //куда должна лететь пуля
        inited = true;
    }

    void Update()
    {
        if (inited && isServer)
        {
            transform.Translate((target - transform.position).normalized * 0.04f);

            foreach (var item in Physics2D.OverlapCircleAll(transform.position, 0.5f))
            {
                Player player = item.GetComponent<Player>();
                if (player)
                {
                    if (player.netId != owner)
                    {
                        player.ChangeHealthValue(player.Health - 1); //отнимаем одну жизнь по аналогии с примером SyncVar
                        NetworkServer.Destroy(gameObject); //уничтожаем пулю
                    }
                }
            }

            if (Vector3.Distance(transform.position, target) < 0.1f) //пуля достигла конечной точки
            {
                NetworkServer.Destroy(gameObject); //значит ее можно уничтожить
            }
        }
    }
}
