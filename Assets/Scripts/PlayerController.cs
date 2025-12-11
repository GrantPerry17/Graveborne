using UnityEngine;
using Photon.Pun;
using HashTable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;

public class PlayerController : MonoBehaviourPunCallbacks
{
    private Transform _transform;
    public PhotonView _pv;
    private Rigidbody2D _rb;

    public float speed;
    public float jumpPower;
    public float bulletPower;
    public int hp;

    private GameSceneManager _gm;

    [SerializeField] private Image hp_image;
    [SerializeField] private TextMeshProUGUI name_Text;

    [Header("Damage Flash")]
    [SerializeField] private SpriteRenderer[] renderersToFlash;
    [SerializeField] private float flashDuration = 0.2f;

    private Color[] originalColors;
    private Color originalHpColor;

    // ---------- COLOR SYSTEM ----------
    private Color assignedColor;
    private bool hasColor = false;
    private bool colorApplied = false;
    // ----------------------------------

    void Start()
    {
        _transform = transform;
        _pv = GetComponent<PhotonView>();
        _rb = GetComponent<Rigidbody2D>();
        _gm = GameObject.Find("GameSceneManager").GetComponent<GameSceneManager>();

        hp = 100;
        name_Text.text = _pv.Owner.NickName;

        if (renderersToFlash != null && renderersToFlash.Length > 0)
        {
            originalColors = new Color[renderersToFlash.Length];
            for (int i = 0; i < renderersToFlash.Length; i++)
                originalColors[i] = renderersToFlash[i].color;
        }

        if (hp_image != null)
            originalHpColor = hp_image.color;

        // Master assigns evenly spaced colors for ALL players
        if (PhotonNetwork.IsMasterClient && _pv.IsMine)
            StartCoroutine(AssignEvenlySpacedColorsAfterJoin());

        // Apply color if already assigned
        ApplyColorFromProperties();
    }

    // -----------------------------------------
    // MASTER assigns color list with large spacing
    // -----------------------------------------
    IEnumerator AssignEvenlySpacedColorsAfterJoin()
    {
        yield return new WaitForSeconds(0.25f); // let players spawn

        Player[] players = PhotonNetwork.PlayerList;

        // Generate evenly spaced colors
        int count = players.Length;
        Color[] palette = GenerateEvenlySpacedPalette(count);

        for (int i = 0; i < players.Length; i++)
        {
            bool giveColor = Random.value > 0.20f; // 20% chance default

            if (!giveColor)
            {
                // Default sprite (no color)
                SetPlayerColorProperties(players[i], 0, 0, 0, false);
                continue;
            }

            Color c = palette[i];
            SetPlayerColorProperties(players[i], c.r, c.g, c.b, true);
        }
    }

    // evenly spaced colors around HSV wheel
    Color[] GenerateEvenlySpacedPalette(int count)
    {
        Color[] colors = new Color[count];
        float step = 1f / count;

        for (int i = 0; i < count; i++)
        {
            float h = i * step;        // different hue for each
            float s = 0.9f;            // strong saturation
            float v = 0.95f;           // bright

            colors[i] = Color.HSVToRGB(h, s, v);
        }

        return colors;
    }

    void SetPlayerColorProperties(Player player, float r, float g, float b, bool enabled)
    {
        HashTable table = new HashTable()
        {
            { "clr_r", r },
            { "clr_g", g },
            { "clr_b", b },
            { "clr_enabled", enabled }
        };

        player.SetCustomProperties(table);
    }

    void ApplyColorFromProperties()
    {
        if (!_pv.Owner.CustomProperties.ContainsKey("clr_enabled"))
            return;

        hasColor = (bool)_pv.Owner.CustomProperties["clr_enabled"];

        if (!hasColor)
        {
            // Restore default sprite colors
            for (int i = 0; i < renderersToFlash.Length; i++)
                renderersToFlash[i].color = originalColors[i];

            colorApplied = true;
            return;
        }

        float r = (float)_pv.Owner.CustomProperties["clr_r"];
        float g = (float)_pv.Owner.CustomProperties["clr_g"];
        float b = (float)_pv.Owner.CustomProperties["clr_b"];

        assignedColor = new Color(r, g, b);

        foreach (var rSprite in renderersToFlash)
            rSprite.color = assignedColor;

        colorApplied = true;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, HashTable changedProps)
    {
        if (targetPlayer == _pv.Owner)
        {
            if (changedProps.ContainsKey("hp"))
            {
                hp = (int)changedProps["hp"];
                UpdateHpBar();
            }

            if (changedProps.ContainsKey("clr_enabled") ||
                changedProps.ContainsKey("clr_r") ||
                changedProps.ContainsKey("clr_g") ||
                changedProps.ContainsKey("clr_b"))
            {
                ApplyColorFromProperties();
            }
        }
    }

    // -----------------------------------------
    // MOVEMENT & SHOOTING
    // -----------------------------------------
    void Update()
    {
        if (_pv.IsMine)
        {
            Control();
            if (_transform.position.y < -5f) Dead();
        }
    }

    void Control()
    {
        float moveX = 0, moveY = 0;
        if (Input.GetKey(KeyCode.LeftArrow)) moveX = -1;
        if (Input.GetKey(KeyCode.RightArrow)) moveX = 1;
        if (Input.GetKey(KeyCode.UpArrow)) moveY = 1;
        if (Input.GetKey(KeyCode.DownArrow)) moveY = -1;

        _transform.position += new Vector3(moveX, moveY, 0) * speed * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space))
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpPower);

        if (Input.GetKeyDown(KeyCode.X))
        {
            Vector3 offset = new Vector3(0.1f, 0, 0);
            GameObject bulletObj = PhotonNetwork.Instantiate("PhotonBullet", _transform.position + offset, Quaternion.identity);
            Rigidbody2D brb = bulletObj.GetComponent<Rigidbody2D>();
            brb.AddForce(new Vector2(bulletPower, 0));
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!_pv.IsMine) return;
        if (!other.gameObject.CompareTag("Bullet")) return;

        PhotonView bulletPV = other.gameObject.GetComponent<PhotonView>();
        if (bulletPV == null || bulletPV.IsMine) return;

        TakeDamage(10);

        string msg = $"<color=#FF4C4C>{bulletPV.Owner.NickName}</color> hit <color=#4C9BFF>{_pv.Owner.NickName}</color>";
        _gm.CallRpcSendMessageToAll(msg);
    }

    void TakeDamage(int damage)
    {
        hp -= damage;

        HashTable table = new HashTable { { "hp", hp } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(table);

        if (_pv != null)
            _pv.RPC("RpcFlashRed", RpcTarget.All);

        if (hp <= 0) Dead();
    }

    [PunRPC]
    private void RpcFlashRed()
    {
        StartCoroutine(FlashRed());
    }

    private IEnumerator FlashRed()
    {
        if (renderersToFlash != null)
            foreach (var r in renderersToFlash)
                if (r != null) r.color = Color.red;

        if (hp_image != null)
            hp_image.color = Color.red;

        yield return new WaitForSeconds(flashDuration);

        if (!hasColor)
        {
            for (int i = 0; i < renderersToFlash.Length; i++)
                renderersToFlash[i].color = originalColors[i];
        }
        else
        {
            foreach (var rSprite in renderersToFlash)
                rSprite.color = assignedColor;
        }

        if (hp_image != null)
            hp_image.color = originalHpColor;
    }

    public void Dead()
    {
        if (_pv.IsMine)
        {
            _gm.CallRpcLocalPlayerDead();
            PhotonNetwork.Destroy(this.gameObject);
        }
    }

    public void UpdateHpBar()
    {
        if (hp_image != null)
        {
            float percent = (float)hp / 100f;
            hp_image.transform.localScale =
                new Vector3(percent, hp_image.transform.localScale.y, hp_image.transform.localScale.z);
        }
    }
}
