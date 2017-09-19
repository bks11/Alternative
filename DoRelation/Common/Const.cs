using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoRelation.Common
{
    public class Const
    {
        public const string DEFALUT_CONNECTION_NAME = @"Data Source=localhost;Initial Catalog=FS_Monitor;Integrated Security=True; MultipleActiveResultSets=True";
        public const string LOG_PATH_NAME = "logFilePath";
        
        #region Queries
        public const string GET_TESTS_QUERY = "SELECT * FROM Test where isArchived != 1";

        public const string GET_TEST_NAME = "SELECT * FROM Test WHERE id = {0}";

        public const string CHECK_USER_DATA_QUERY = "SELECT TOP(1) * FROM RegistrationUsers WHERE firstName = '{0}' AND lastName = '{1}' AND groupNumber = '{2}' AND testId = {3} ORDER BY id DESC";

        public const string REGISTER_USER_DATA_QUERY = "INSERT INTO RegistrationUsers(firstName, lastName, groupNumber, testId, registrationTime) VALUES('{0}', '{1}', '{2}', {3}, GETDATE());";

        public const string GET_NEW_USER_DATA_QUERY = "SELECT TOP(1) id FROM RegistrationUsers ORDER BY id DESC; ";

        public const string BIND_USER_ANSWERS = "INSERT INTO AnswerUser (userId, questionId) SELECT DISTINCT {0}, q.id as questionId FROM Question AS q INNER JOIN Answer AS a ON q.id = a.questionId WHERE testId = {1}";

        public const string GET_USER_OPENED_QUESTION_QUERY = "SELECT q.id, q.text FROM AnswerUser au INNER JOIN Question q ON au.questionId = q.id WHERE au.userId = {0} and q.testId = {1} and au.answerTime is null";

        public const string GET_USER_OPENED_QUESTION_ANSWES_QUERY = "SELECT DISTINCT q.id as questionId, a.id as answerId, a.text FROM AnswerUser au INNER JOIN Question q ON au.questionId = q.id INNER JOIN Answer a ON q.id = a.questionId WHERE au.userId = {0} and q.testId = {1} and au.answerTime is null";

        public const string SET_ANSWER_FOR_QUESTION_QUERY = "UPDATE AnswerUser SET answerId = {0}, answerTime = GETDATE() WHERE userId = {1} AND questionId = {2}";

        public const string CLOSE_TEST_QUERY = "UPDATE RegistrationUsers SET testIsFinished = 1, testResult = {2} WHERE id = {0} AND testId = {1}";

        public const string GET_TEST_RESULTS_QUERY = "SELECT t.name, ru.testResult FROM RegistrationUsers ru INNER JOIN Test t ON ru.testId = t.id WHERE ru.id = {0} AND t.id = {1};";

        public const string CHECK_TEST_IS_FINISHED = "SELECT COALESCE(testIsFinished, 0) as testIsFinished FROM RegistrationUsers WHERE id = {0} AND testId = {1}";

        public const string GET_USER_DATA_BY_ID_QUERY = "SELECT * FROM RegistrationUsers ru INNER JOIN Test t ON ru.testId = t.id WHERE ru.id = {0}";

        public const string GET_TEST_QUESTION_COUNT_QUERY = "SELECT questionCount FROM Test WHERE id = {0}";

        public const string GET_TEST_RESULT_QUERY = @"SELECT COUNT(a.isCorrect) AS isCorrectCount
                                                    FROM AnswerUser AS au 
                                                    INNER JOIN Answer AS a ON a.id = au.answerId 
                                                    INNER JOIN Question AS q ON q.id = au.questionId 
                                                    WHERE q.testId = {0} AND a.isCorrect = 1 and au.userId = {1}";

        public const string GET_ALL_RESULTS_BY_USER_AND_TEST_QUERY = @"SELECT distinct t.name AS testName, ru.registrationTime AS regTime, ru.testIsFinished, COALESCE(ru.testResult, 0) as testResult
FROM AnswerUser AS au LEFT OUTER JOIN
Answer AS a ON a.id = au.answerId INNER JOIN
Question AS q ON q.id = au.questionId INNER JOIN
RegistrationUsers AS ru ON ru.id = au.userId INNER JOIN
Test AS t ON q.testId = t.id
WHERE (ru.lastName = '{0}') AND (ru.firstName = '{1}') AND (ru.groupNumber = {2}) AND (q.testId = {3})
";
        public const string GET_ALL_RESULT_QUERY = "SELECT t.name, ru.testIsFinished, ru.testResult, ru.registrationTime as regTime FROM RegistrationUsers ru INNER JOIN Test t ON ru.testId = t.id WHERE firstName = '{0}' AND lastName = '{1}' AND groupNumber = '{2}' AND testId = {3};";
        #endregion Queries

        #region Session Parameters

        public const string SN_OPENED_QUESTIONS = "userOpenedQuestions";
        
        public const string SN_SKIPPED_QUESTIONS = "userSkippedQuestions";

        #endregion Session Parameters
    }
}
