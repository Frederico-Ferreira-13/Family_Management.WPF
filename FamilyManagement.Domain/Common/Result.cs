using System;
using FamilyManagement.Domain.Errors;

namespace FamilyManagement.Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && !error.IsNone)
        {
            throw new InvalidOperationException(
                "Um resultado de sucesso não pode conter um erro.");
        }

        if (!isSuccess && error.IsNone)
        {
            throw new InvalidOperationException(
                "Um resultado de falha deve conter um erro.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success()
    {
        return new Result(
            isSuccess: true,
            error: Error.None);
    }

    public static Result Failure(Error error)
    {
        return new Result(
            isSuccess: false,
            error: error);
    }

    public static implicit operator Result(Error error)
    {
        return Failure(error);
    }
}

public class Result<T> : Result
{
    private readonly T? _value;

    public T Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException(
                    "Não é possível aceder ao valor de um resultado de falha.");
            }

            return _value!;
        }
    }

    protected Result(
        bool isSuccess,
        T? value,
        Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(
            isSuccess: true,
            value: value,
            error: Error.None);
    }

    public new static Result<T> Failure(Error error)
    {
        return new Result<T>(
            isSuccess: false,
            value: default,
            error: error);
    }

    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }

    public static implicit operator Result<T>(Error error)
    {
        return Failure(error);
    }
}