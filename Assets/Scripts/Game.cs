﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

public class Game : MonoBehaviour
{
	private int counter = 0;
    /// <summary>
    /// Variable used for game update delay calcuations.
    /// </summary>
    private float time;

    /// <summary>
    /// Holds last started bonus placing coroutine.
    /// </summary>
    private IEnumerator bonusCoroutine;

    /// <summary>
    /// Object responisble for managing sound effects.
    /// </summary>
    private SoundManager soundManager;

    private Snake snake;

    /// <summary>
    /// Position of an apple (1 point fruit).
    /// </summary>
    private IntVector2 applePosition;

    /// <summary>
    /// Position of 10 point (bonus) fruit.
    /// </summary>
    private IntVector2 bonusPosition;

    /// <summary>
    /// Position of a wall.
    /// </summary>
    private IntVector2 wallPosition;
    /// <summary>
    /// Specifies if bonus is visible and active.
    /// </summary>

    private bool bonusActive;

    /// <summary>
    /// Game controller.
    /// </summary>
    private Controller controller;

    /// <summary>
    /// Menu panel.
    /// </summary>
    public MenuPanel Menu;
    /// <summary>
    /// Game over panel.
    /// </summary>
    public GameOverPanel GameOver;
    /// <summary>
    /// Main game panel (with board).
    /// </summary>
    public GamePanel GamePanel;

    public NextLevelMenu NextLevelMenu;

    public EndGameMenu EndGameMenu;
    public PauseMenu PauseMenu;
    /// <summary>
    /// Parameter specyfying delay between snake movements (in seconds).
    /// </summary>
    [Range(0f, 3f)]
    public float GameSpeed;

    /// <summary>
    /// Has to be set to game's board object.
    /// </summary>
    public Board Board;

    private int _score;
    private int _highScore;
    private int tempScore = 0;
    public int level = 0;
    public bool dead = false;
    /// <summary>
    /// Current score.
    /// </summary>
    public int Score
    {
        get
        {
            return _score;
        }
        set
        {
            _score = value;
            GamePanel.Score = value;
            GameOver.Score = value;

            if (value > HighScore)
            {
                HighScore = value;
            }
        }
    }

    /// <summary>
    /// Current high score.
    /// </summary>
    public int HighScore
    {
        get
        {
            return _highScore;
        }
        set
        {
            _highScore = value;
            PlayerPrefs.SetInt("High Score", value);
            //GamePanel.HighScore = value;
        }
    }

    /// <summary>
    /// Determines if games is paused (when true) or running (when false).
    /// </summary>
    public bool Paused { get; private set; }

	public void InterruptGame()
	{
		StopCoroutine(bonusCoroutine);
		Paused = true;
	}

    // Use this for initialization
    void Start()
    {
        // Display current high score.
        HighScore = PlayerPrefs.GetInt("High Score", 0);

		// Find sound manager
        soundManager = GetComponent<SoundManager>();

        // Show main menu
        ShowMenu();

        // Set controller
        controller = GetComponent<Controller>();

        // Creates snake
        snake = new Snake(Board);

        // Pause the game
		Paused = true;
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        while (time > GameSpeed)
        {
            time -= GameSpeed;
            UpdateGameState();
            if (Score >= 2 && level == 0 && dead == false)
            {
                tempScore = Score;
                Time.timeScale = 0;
                StopCoroutine(bonusCoroutine);
                NextLevelMenu.gameObject.SetActive(true);
            }

            else if (Score >= 3 && level == 1 && dead == false)
            {
                tempScore = Score;
                Time.timeScale = 0;
                StopCoroutine(bonusCoroutine);
                NextLevelMenu.gameObject.SetActive(true);
            }

            else if (Score >= 5 && level == 2 && dead == false)
            {
                StopCoroutine(bonusCoroutine);
                tempScore = 0;
                level = 0;
                NextLevelMenu.gameObject.SetActive(false);
                EndGameMenu.gameObject.SetActive(true);
            }
        }
    }

    public void Pause()
	{
		Paused = true;
		PauseMenu.gameObject.SetActive(true);
		Time.timeScale = 0;
		soundManager.PlayPause();
	}

	public void Unpause()
	{
		soundManager.StopPause();
		Paused = false;
		PauseMenu.gameObject.SetActive(false);
		controller.Resume();
		Time.timeScale = 1;
	}

	public void QuitGame()
	{
		Application.Quit();
	}

    /// <summary>
    /// Updates game state.
    /// </summary>
    private void UpdateGameState()
    {
		if (!Paused && snake != null)
        {
            var dir = controller.NextDirection();
			var lastdir = controller.PreviousDirection();

			// New head position
			var head = snake.NextHeadPosition(dir);

			if (dir == Vector2.zero)
			{
				head = snake.NextHeadPosition(lastdir);
				Pause();
			}

            var x = head.x;
            var y = head.y;

            if (snake.WithoutTail.Contains(head))
            {
                // Snake has bitten its tail - game over
                dead = true;
                StartCoroutine(GameOverCoroutine());
                return;
            }

            if (x >= 0 && x < Board.Columns && y >= 0 && y < Board.Rows)
            {
				if (head == applePosition)
				{
					soundManager.PlayAppleSoundEffect();
					snake.Move(dir, true);
					Score += 1;
					PlantAnApple();
				}
				else if (head == bonusPosition && bonusActive)
				{
					soundManager.PlayBonusSoundEffect();
					snake.Move(dir, true);
					Score += 10;
					StopCoroutine(bonusCoroutine);
					PlantABonus();
				}

				else if (Board[head].Content == TileContent.Wall)
				{
					StartCoroutine(GameOverCoroutine());
				}
				else
				{
					snake.Move(dir, false);
					//counter++;
					//if (counter == 1) //Frequency of beeps
					//{
					//	if (((applePosition.y <= snake.Head.y && applePosition.x > snake.Head.x) || (applePosition.y >= snake.Head.y && applePosition.x > snake.Head.x)) && (lastdir == Vector2.up))
					//	{
					//		soundManager.PlayRightBeepSoundEffect();
					//	}
					//	else if (((applePosition.y <= snake.Head.y && applePosition.x < snake.Head.x) || (applePosition.y >= snake.Head.y && applePosition.x < snake.Head.x)) && (lastdir == Vector2.up))
					//	{
					//		soundManager.PlayLeftBeepSoundEffect();
					//	}
					//	else if ((applePosition.y <= snake.Head.y && applePosition.x == snake.Head.x) && (lastdir == Vector2.up))
					//	{
					//		soundManager.PlayCenterBeepSoundEffect();
					//	}
					//	else if (((applePosition.y > snake.Head.y && applePosition.x <= snake.Head.x) || (applePosition.y > snake.Head.y && applePosition.x >= snake.Head.x)) && (lastdir == Vector2.right))
					//	{
					//		soundManager.PlayRightBeepSoundEffect();
					//	}
					//	else if (((applePosition.y < snake.Head.y && applePosition.x <= snake.Head.x) || (applePosition.y < snake.Head.y && applePosition.x >= snake.Head.x)) && (lastdir == Vector2.right))
					//	{
					//		soundManager.PlayLeftBeepSoundEffect();
					//	}
					//	else if ((applePosition.y == snake.Head.y && applePosition.x >= snake.Head.x) && (lastdir == Vector2.right))
					//	{
					//		soundManager.PlayCenterBeepSoundEffect();
					//	}
					//	else if (((applePosition.y < snake.Head.y && applePosition.x >= snake.Head.x) || (applePosition.y < snake.Head.y && applePosition.x <= snake.Head.x)) && (lastdir == Vector2.left))
					//	{
					//		soundManager.PlayRightBeepSoundEffect();
					//	}
					//	else if (((applePosition.y > snake.Head.y && applePosition.x >= snake.Head.x) || (applePosition.y > snake.Head.y && applePosition.x <= snake.Head.x)) && (lastdir == Vector2.left))
					//	{
					//		soundManager.PlayLeftBeepSoundEffect();
					//	}
					//	else if ((applePosition.y == snake.Head.y && applePosition.x <= snake.Head.x) && (lastdir == Vector2.left))
					//	{
					//		soundManager.PlayCenterBeepSoundEffect();
					//	}
					//	else if (((applePosition.y >= snake.Head.y && applePosition.x > snake.Head.x) || (applePosition.y <= snake.Head.y && applePosition.x > snake.Head.x)) && (lastdir == Vector2.down))
					//	{
					//		soundManager.PlayRightBeepSoundEffect();
					//	}
					//	else if (((applePosition.y >= snake.Head.y && applePosition.x < snake.Head.x) || (applePosition.y <= snake.Head.y && applePosition.x < snake.Head.x)) && (lastdir == Vector2.down))
					//	{
					//		soundManager.PlayLeftBeepSoundEffect();
					//	}
					//	else if ((applePosition.y >= snake.Head.y && applePosition.x == snake.Head.x) && (lastdir == Vector2.down))
					//	{
					//		soundManager.PlayCenterBeepSoundEffect();
					//	}
					//	counter = 0;
					//}
                }
            }

            else
            {
                // Head is outside board's bounds - game over.
                dead = true;
                StartCoroutine(GameOverCoroutine());
            }
        }
    }

    /// <summary>
    /// Shows main menu.
    /// </summary>
    public void ShowMenu()
    {
		soundManager.StopGameOver();
		soundManager.StopPause();
        HideAllPanels();
        Menu.gameObject.SetActive(true);
		soundManager.PlayMainMenu();
    }

    /// <summary>
    /// Shows game over panel.
    /// </summary>
    public void ShowGameOver()
    {
		soundManager.StopMainMenu();
		soundManager.StopPause();
        HideAllPanels();
        GameOver.gameObject.SetActive(true);
		soundManager.PlayGameOver();
    }

    /// <summary>
    /// Shows the board and starts the game.
    /// </summary>
    public void StartGame()
    {
        HideAllPanels();
        Restart();
        dead = false;
        BuildMultipleWallOne();
        GamePanel.gameObject.SetActive(true);
    }

    /// <summary>
    /// Hides all panels.
    /// </summary>
    private void HideAllPanels()
    {
		soundManager.StopGameOver();
		soundManager.StopPause();
		soundManager.StopMainMenu();
        Menu.gameObject.SetActive(false);
        GamePanel.gameObject.SetActive(false);
        GameOver.gameObject.SetActive(false);
		PauseMenu.gameObject.SetActive(false);
        NextLevelMenu.gameObject.SetActive(false);
        EndGameMenu.gameObject.SetActive(false);
    }

    /// <summary>
    /// Returns game to initial conditions.
    /// </summary>
    private void Restart()
    {
		counter = 0;
        // Resets the controller.
        controller.Reset();

        // Set score
        Score = 0;

        // Disable bonus
        bonusActive = false;

        // Clear board
        Board.Reset();

        // Resets snake
        snake.Reset();

        // Plant an apple
        PlantAnApple();

        // Start bonus coroutine
        PlantABonus();

		Time.timeScale = 1;

        // Start the game
        Paused = false;
        time = 0;
    }

    /// <summary>
    /// Starts bonus placing coroutine
    /// </summary>
    private void PlantABonus()
    {
        bonusActive = false;
        bonusCoroutine = BonusCoroutine();
        StartCoroutine(bonusCoroutine);
    }

    /// <summary>
    /// Puts an apple in new position.
    /// </summary>
    private void PlantAnApple()
    {
	var x = 0;
        if (Board[applePosition].Content == TileContent.Apple)
        {
            Board[applePosition].Content = TileContent.Empty;
        }

        var emptyPositions = Board.EmptyPositions.ToList();
        if (emptyPositions.Count == 0)
        {
            return;
        }
        applePosition = emptyPositions.RandomElement();

		while (x<1)
		{
			if(Board[applePosition].Content == TileContent.Wall)
			{
				applePosition = emptyPositions.RandomElement();
			}

			else if(Board[applePosition].Content != TileContent.Wall)
			{
				break;
			}
		}

        Board[applePosition].Content = TileContent.Apple;
    }

    /// <summary>
    /// Couroutine responsible for placing and removing bonus from the board.
    /// It waits for a random period of time, puts the bonus on the board, and then removes it after constant delay.
    /// </summary>
    /// <returns></returns>
    private IEnumerator BonusCoroutine()
    {
        // Wait for a random period of time
        yield return new WaitForSeconds(Random.Range(GameSpeed * 20, GameSpeed * 40));

        // Put a bonus on a board at a random place
        var emptyPositions = Board.EmptyPositions.ToList();
        if (emptyPositions.Count == 0)
        {
            yield break;
        }
        bonusPosition = emptyPositions.RandomElement();
        Board[bonusPosition].Content = TileContent.Bonus;
        bonusActive = true;

        // Wait
        yield return new WaitForSeconds(GameSpeed * 16);

        // Start bonus to blink
        for (int i = 0; i < 5; i++)
        {
            Board[bonusPosition].ContentHidden = true;
            yield return new WaitForSeconds(GameSpeed * 1.5f);
            Board[bonusPosition].ContentHidden = false;
            yield return new WaitForSeconds(GameSpeed * 1.5f);
        }

        // Remove a bonus and restart the coroutine
        bonusActive = false;
        Board[bonusPosition].Content = TileContent.Empty;

        bonusCoroutine = BonusCoroutine();
        yield return StartCoroutine(bonusCoroutine);
    }

    /// <summary>
    /// Courotine that is started when game is over. Causes snake to blink and then shows game over panel.
    /// </summary>
    /// <returns></returns>
    private IEnumerator GameOverCoroutine()
    {
        // Play game over sound effect
        soundManager.PlayGameOverSoundEffect();

        // Stop bonus coroutine
        StopCoroutine(bonusCoroutine);

        // Pause the game
        Paused = true;

        // Start snake blinking
        for (int i = 0; i < 3; i++)
        {
            snake.Hide();
            yield return new WaitForSeconds(GameSpeed * 1.5f);
            snake.Show();
            yield return new WaitForSeconds(GameSpeed * 1.5f);
        }

        // Show "game over" panel
        ShowGameOver();
        level = 0;
        dead = true;
    }

    private void BuildAWall(int x, int y)
    {
        wallPosition.x = x;
        wallPosition.y = y;
        Board[wallPosition].Content = TileContent.Wall;
    }

    //Build multiple wall by calling the BuildAWall function inside this function and pass in x-axis and y-axis value.
    private void BuildMultipleWallOne()
    {
        for (int x = 6; x < 11; x++)
        {
            BuildAWall(x, 4);
            BuildAWall(x, 16);
        }

        for (int x = 19; x < 24; x++)
        {
            BuildAWall(x, 4);
            BuildAWall(x, 16);
        }

        for (int y = 5; y < 9; y++)
        {
            BuildAWall(6, y);
            BuildAWall(23, y);
        }

        for (int y = 12; y < 16; y++)
        {
            BuildAWall(6, y);
            BuildAWall(23, y);
        }

        for (int y = 7; y < 14; y++)
        {
            BuildAWall(15, y);
        }

        for (int x = 12; x < 19; x++)
        {
            BuildAWall(x, 10);
        }
    }

    private void BuildMultipleWallTwo()
    {
        for (int y = 4; y < 9; y++)
        {
            BuildAWall(12, y);
            BuildAWall(18, y);
        }

        for (int y = 12; y < 17; y++)
        {
            BuildAWall(12, y);
            BuildAWall(18, y);
        }

        for (int x = 7; x < 12; x++)
        {
            BuildAWall(x, 8);
            BuildAWall(x, 12);
        }

        for (int x = 19; x < 24; x++)
        {
            BuildAWall(x, 8);
            BuildAWall(x, 12);
        }

        BuildAWall(15, 10);
    }

    private void BuildMultipleWallThree()
    {
        for (int y = 0; y < 5; y++)
        {
            BuildAWall(12, y);
            BuildAWall(18, y);
        }

        for (int y = 15; y < 20; y++)
        {
            BuildAWall(12, y);
            BuildAWall(18, y);
        }

        for (int x = 0; x < 4; x++)
        {
            BuildAWall(x, 5);
            BuildAWall(x, 15);
        }

        for (int x = 26; x < 30; x++)
        {
            BuildAWall(x, 5);
            BuildAWall(x, 15);
        }

        for (int x = 14; x < 17; x++)
        {
            BuildAWall(x, 8);
            BuildAWall(x, 12);
        }

        for (int y = 8; y < 13; y++)
        {
            BuildAWall(10, y);
            BuildAWall(20, y);
            BuildAWall(2, y);
            BuildAWall(28, y);
        }
    }

    private void LoadNextLevel()
    {

        // Resets the controller.
        controller.Reset();

        // Set score
        Score = 0;

        // Disable bonus
        bonusActive = false;

        // Clear board
        Board.Reset();

        // Resets snake
        snake.Reset();

        //Second level
        if (tempScore >= 2 && level == 0)
        {
            level = 1;
            tempScore = 0;
            BuildMultipleWallTwo();
        }

        //Last level
        else if (tempScore >= 3 && level == 1)
        {
            level = 2;
            tempScore = 0;
            BuildMultipleWallThree();
        }

        // Plant an apple
        PlantAnApple();

        // Start bonus coroutine
        PlantABonus();

        Time.timeScale = 1;

        // Start the game
        Paused = false;
        time = 0;
    }

    public void StartNextLevel()
    {
        HideAllPanels();
        LoadNextLevel();
        GamePanel.gameObject.SetActive(true);
    }
}
