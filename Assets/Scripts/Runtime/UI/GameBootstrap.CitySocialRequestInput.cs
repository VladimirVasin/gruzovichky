using UnityEngine;
using UnityEngine.EventSystems;

public partial class GameBootstrap
{
    private const float CitySocialTopicRejectShakeSeconds = 0.34f;
    private Vector2 citySocialActionButtonBasePosition;
    private float citySocialTopicRejectShakeTimer;

    private void ResetCitySocialTopicInputFeedback()
    {
        citySocialTopicRejectShakeTimer = 0f;
    }

    private void SubmitCitySocialTopic()
    {
        if (string.IsNullOrWhiteSpace(citySocialRequestSceneHud.TopicInput.text))
        {
            RejectEmptyCitySocialTopic();
            return;
        }

        citySocialRequestTopic = SanitizeCitySocialTopic(citySocialRequestSceneHud.TopicInput.text);
        citySocialRequestSceneHud.TopicInput.DeactivateInputField();
        citySocialRequestSceneHud.TopicInput.gameObject.SetActive(false);
        citySocialRequestSceneHud.TargetCard.gameObject.SetActive(true);
        citySocialRequestSceneHud.TargetGroup.alpha = 0f;
        citySocialRequestSceneHud.TargetCard.anchoredPosition = new Vector2(360f, 92f);
        citySocialRequestSceneHud.TargetCard.localScale = Vector3.one * 0.74f;
        citySocialRequestSceneHud.TitleText.text = "Разговор начинается";
        citySocialSpeakingSide = -1;
        SetCitySocialBodyText("Второй участник уже здесь. Сейчас город попробует сделать вид, что это случайная беседа, а не маленькая операция Ратуши.");
        citySocialRequestSceneHud.ActionButtonText.text = "Дальше";
        citySocialRequestScenePhase = CitySocialRequestScenePhase.TargetReveal;
        citySocialRequestSceneTimer = 0f;
        PlayUiSound(uiSelectClip, 0.78f);
    }

    private void RejectEmptyCitySocialTopic()
    {
        RectTransform buttonRect = citySocialRequestSceneHud.ActionButton.GetComponent<RectTransform>();
        citySocialActionButtonBasePosition = buttonRect.anchoredPosition;
        citySocialTopicRejectShakeTimer = CitySocialTopicRejectShakeSeconds;
        citySocialRequestSceneHud.TopicInput.ActivateInputField();
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(citySocialRequestSceneHud.TopicInput.gameObject);
        }

        PlayCitySocialTopicRejectSound();
    }

    private void UpdateCitySocialTopicRejectShake(float dt)
    {
        if (citySocialTopicRejectShakeTimer <= 0f || citySocialRequestSceneHud?.ActionButton == null)
        {
            return;
        }

        citySocialTopicRejectShakeTimer = Mathf.Max(0f, citySocialTopicRejectShakeTimer - dt);
        RectTransform buttonRect = citySocialRequestSceneHud.ActionButton.GetComponent<RectTransform>();
        float t = 1f - citySocialTopicRejectShakeTimer / CitySocialTopicRejectShakeSeconds;
        float offsetX = Mathf.Sin(t * Mathf.PI * 9f) * 14f * (1f - t);
        buttonRect.anchoredPosition = citySocialActionButtonBasePosition + new Vector2(offsetX, 0f);
        if (citySocialTopicRejectShakeTimer <= 0f)
        {
            buttonRect.anchoredPosition = citySocialActionButtonBasePosition;
        }
    }
}
