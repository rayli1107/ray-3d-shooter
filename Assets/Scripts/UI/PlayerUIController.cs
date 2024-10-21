using UnityEngine;
using UnityEngine.UI;

public class PlayerUIController : MonoBehaviour
{
    [SerializeField]
    private Slider _sliderHP;

    [SerializeField]
    private Image _imageTarget;

    public bool IsTarget
    {
        get => _imageTarget.enabled;
        set { _imageTarget.enabled = value; }
    }

    private PlayerController _player;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        _player.playerData.OnValueChanged += onPlayerDataValueChange;
        onPlayerDataValueChange(null, _player.playerData.Value);
    }

    private void OnDisable()
    {
        _player.playerData.OnValueChanged -= onPlayerDataValueChange;
    }

    private void onPlayerDataValueChange(PlayerData _, PlayerData playerData)
    {
        if (playerData != null)
        {
            _sliderHP.maxValue = playerData.maxHP;
            _sliderHP.value = playerData.currentHP;
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}
