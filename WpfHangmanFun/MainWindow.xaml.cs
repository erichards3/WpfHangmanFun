using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using System.Net.Http;
using HangmanFunWPF.Classes;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace HangmanFunWPF
{
  public partial class MainWindow : Window
  {

    #region Private Variables

    /// <summary>
    /// Contains the http client used to reach out the to web api for a random word
    /// </summary>
    private static HttpClient httpClient = new HttpClient();

    /// <summary>
    /// Contains the base Url for the web api
    /// </summary>
    private static string baseUrl = @"http://api.wordnik.com/v4/words.json/randomWords?";

    // Stores the parameters for the web api call
    // Program could be modified to allow user to adjust these
    bool hasDictionaryDefinition = true;
    int minCorpusCount = 0;
    int minLength = 5;
    int maxLength = 15;
    int limit = 1;
    string apiKey = "516c9245fbdb0d2b3731a0381550bad6b8ed35a122ef16191";

    /// <summary>
    /// Contains the letters of the random word
    /// </summary>
    private List<char> secretLetters = new List<char>();

    /// <summary>
    /// Contains the masked letters to display to the user
    /// </summary>
    private List<char> displayedLetters = new List<char>();

    /// <summary>
    /// Keeps track of the number of incorect guesses to control game flow
    /// </summary>
    int wrongGuesses = 0;

    /// <summary>
    /// Keep track of the number of times the user has won
    /// </summary>
    int userWins = 0;

    /// <summary>
    /// Keep track of the number of times the user has lost
    /// </summary>
    int userLosses = 0;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class
    /// </summary>
    public MainWindow()
    {
      InitializeComponent();

      // Run the setup in another thread because the http client 
      // should be used in an async method
      Task t = new Task(RunSetup);
      t.Start();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes the values on the view and retrieves a random word  
    /// from the wordnik.com web api
    /// </summary>
    private async void RunSetup()
    {
      // Put the parameters together for the api call
      StringBuilder parameters = new StringBuilder();
      parameters.Append($"hasDictionaryDef={hasDictionaryDefinition}");
      parameters.Append($"&minCorpusCount={minCorpusCount}");
      parameters.Append($"&minLength={minLength}");
      parameters.Append($"&maxLength={maxLength}");
      parameters.Append($"&limit={limit}");
      parameters.Append($"&api_key={apiKey}");

      var responseData = await httpClient.GetAsync(baseUrl + parameters.ToString()).ConfigureAwait(false);
      var responseText = await responseData.Content.ReadAsStringAsync().ConfigureAwait(false);

      /* This is where some magic happens. The web api returns a Json object containing the 
       * random word. Using the Newtonsoft.Json library, I can really simply deserialize the
       * Json object into a C#.Net object. Check the  RandomWord class definition. */
      RandomWord randomWord =
        JsonConvert.DeserializeObject<RandomWord>(
          responseText.TrimStart('[').TrimEnd(']')); // Trim is needed for correct parsing

      // Put all the letters of the random word into the list of secret letters
      foreach (var c in randomWord.Word)
        secretLetters.Add(char.ToUpper(c));

      // Mask the letters so the user has to guess
      displayedLetters = PrepareLetters(secretLetters);

      // Need to use Dispatcher because the UI is on a different thread
      this.Dispatcher.Invoke(() =>
      {
        // A custom extension method spaces the characters nicely. Check Extensions.cs for details.
        secretWord.Text = displayedLetters.ToHangmanString();
        // Enables the buttons after a word has been set
        EnableButtons();
      });
    }

    /// <summary>
    /// Masks the word after a random word is pulled from the api
    /// </summary>
    /// <param name="list">The list of letters to be masked</param>
    /// <returns>A <see cref="List{char}"/> containing an underscore in place of alphabet characters</returns>
    private List<char> PrepareLetters(List<char> list)
    {
      // Nothing to do here
      if (list.Count == 0)
        return new List<char>();

      // Declare a variable to be returned
      var result = new List<char>();

      // Now, loop through each of the letters and replace alphabet characters with an underscore
      // but leave all other characters (such as hyphen or apostrophe) unmodified
      foreach (var c in list)
      {
        if (Regex.Match(c.ToString(), @"[a-zA-Z]").Success)
          result.Add('_');
        else
          result.Add(c);
      }

      return new List<char>(result);
    }

    private void EnableButtons()
    {
      // Loop through all the buttons and enable them
      foreach (var c in this.gridMain.Children)
      {
        Button b = c as Button;
        if (b != null)
          b.IsEnabled = true;
      }
    }

    /// <summary>
    /// Executes when a letter is selected from the view
    /// </summary>
    /// <param name="sender">The <see cref="Button"/> on which the user clicked</param>
    /// <param name="e">Event Arguments</param>
    private void LetterSelected(object sender, RoutedEventArgs e)
    {
      // Handle the case in which the user lost, then clicked "No" 
      // to starting a new game and then chooses another letter
      if (wrongGuesses >= 6)
        return;

      Button b = (sender as Button); // Get our button object
      char letterGuessed = b.Content.ToString()[0]; // Get the letter that was clicked on
      b.IsEnabled = false; // Disable the button so it can't be selected again

      // If the guess is wrong
      if (!secretLetters.Contains(letterGuessed))
      {
        // Increment the wrong guesses counter and update the image
        wrongGuesses++;
        personImage.Source = new BitmapImage(new Uri($@"/Images/image_{wrongGuesses + 1}.png", UriKind.Relative));

        // Check to see if the user has lost
        if (wrongGuesses == 6)
        {
          userLosses++;

          // Get the word so that the user can know what it was
          StringBuilder sb = new StringBuilder();
          secretLetters.ForEach(l => sb.Append(l));

          // Display a taunting message to the user and ask if they want to play again
          if (MessageBox.Show(
            $"Muahaha! Your skills are inferior to mine!\n\nThe secret word was: {sb.ToString()}\n\nPlay again?", "You Lose!",
            MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            StartNewGame(sender, e); // If they do, reset

          SetWinLossText();
        }

        // Now return so the next portion is not executed
        return;
      }

      // Now figure out which index(es) to reveal the letters in the masked word
      for (int i = 0; i < secretLetters.Count; i++)
      {
        if (secretLetters[i] == letterGuessed)
          displayedLetters[i] = letterGuessed;
      }

      // Set the text on the view. 
      // Note: Here, I don't need to use the Dispatcher because we are in the main thread
      secretWord.Text = displayedLetters.ToHangmanString();

      // Now check to see if the user figured out the word
      if (!displayedLetters.Contains('_'))
      {
        userWins++;

        // Show a message and ask if they want to play again
        if (MessageBox.Show(
            "Congratulations on guessing the word.. I'll try harder next time.\n\nPlay again?", "You Win!",
            MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
          StartNewGame(sender, e); // If they do, reset

        SetWinLossText();
      }
    }

    private void SetWinLossText()
    {
      // Update the wins/losses on the view
      numberWins.Text = $"Total Wins: {userWins}";
      numberLosses.Text = $"Total Losses: {userLosses}";
    }

    /// <summary>
    /// Handles resetting the elements on the screen so that the user can play again
    /// </summary>
    /// <param name="sender">the button selected</param>
    /// <param name="e">The event arguments</param>
    private void StartNewGame(object sender, RoutedEventArgs e)
    {
      // I need to clear the values or else the new word just gets
      // added on to the end of the old one... indefinitely!
      secretLetters.Clear();
      displayedLetters.Clear();

      // We can call the Run setup method because it will handle 
      // getting a new word and masking it for us
      Task t = new Task(RunSetup);
      t.Start();

      // Reset the image and the incorrect guesses counter
      personImage.Source = new BitmapImage(new Uri(@"/Images/image_1.png", UriKind.Relative));
      wrongGuesses = 0;
    }

    /// <summary>
    /// Executes when the user clickes the Close button
    /// </summary>
    /// <param name="sender">The close button</param>
    /// <param name="e">The event arguments</param>
    private void ExitGame(object sender, RoutedEventArgs e)
    {
      // Confirm that the user really wants to leave this awesome game
      if (MessageBox.Show("Are you sure you want to quit playing this super fun game?", "Abandon Me?",
        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        this.Close(); // If they do, close it
    }

    #endregion

  }
}
